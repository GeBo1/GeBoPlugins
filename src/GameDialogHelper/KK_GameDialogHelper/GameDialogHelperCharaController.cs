using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GameDialogHelperPlugin.AdvancedLogicMemory;
using GameDialogHelperPlugin.Utilities;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using Manager;
using MessagePack;
using UnityEngine.Assertions;

namespace GameDialogHelperPlugin
{
    public class GameDialogHelperCharaController : CharaCustomFunctionController
    {
        internal static readonly Dictionary<Guid, CharaDialogMemory>
            PersistentMemoryByGuid = new Dictionary<Guid, CharaDialogMemory>();

        internal static readonly Dictionary<SaveData.Heroine, CharaDialogMemory>
            PersistentMemoryByHeroine = new Dictionary<SaveData.Heroine, CharaDialogMemory>();

        internal static readonly Dictionary<Guid, Guid> CharaGuidMap = new Dictionary<Guid, Guid>();

        private readonly HashSet<Guid> _previousGuids = new HashSet<Guid>();

        private CharaDialogMemory _dialogMemory;
        private bool _dialogMemoryInitialized;

        private int _heroineIndex = -1;

        private long _lastPersistedToCard = -1;
        private static ManualLogSource Logger => GameDialogHelper.Logger;

        public long LastPersistedToCard
        {
            get => _lastPersistedToCard;
            private set => _lastPersistedToCard = Math.Max(value, _lastPersistedToCard);
        }

        [PublicAPI]
        public int HeroineIndex
        {
            get
            {
                if (_heroineIndex == -1)
                {
                    ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(h =>
                        Game.Instance.SafeProcObject(i =>
                            i.HeroineList.SafeProc(l => _heroineIndex = l.IndexOf(h)))));
                }

                return _heroineIndex;
            }
        }

        internal CharaDialogMemory DialogMemory
        {
            get
            {
                var result = SelectLatestMemory(
                    GetCurrentMemory,
                    LoadPersistentMemoryByGuid);
                if (result == null)
                {
                    result = new CharaDialogMemory
                    {
                        SaveGuid = GameDialogHelper.Instance.CurrentSaveGuid,
                        SessionGuid = GameDialogHelper.Instance.CurrentSessionGuid,
                        CharaGuidVersion = PluginDataInfo.CurrentCharaGuidVersion,
                        PlayerGuid = GameDialogHelper.Instance.CurrentPlayerGuid
                    };
                    ChaFileControl.SafeProc(
                        cfc => cfc.GetHeroine().SafeProc(h => result.CharaGuid = h.GetCharaGuid()));
                }

                if (result == _dialogMemory) return _dialogMemory;
                _dialogMemory = result;
                PersistToMemory();
                _dialogMemoryInitialized = true;
                return _dialogMemory;
            }

            [UsedImplicitly]
            private set
            {
                _dialogMemory = value;
                PersistToMemory();
            }
        }

        private void ValidateOrClearDialogMemory()
        {
            if (_dialogMemory != null &&
                _dialogMemory.IsValidForCurrentSession($"{GetLogId()} {nameof(ValidateOrClearDialogMemory)}"))
            {
                return;
            }

            _dialogMemory = null;
            _dialogMemoryInitialized = false;
        }

        private CharaDialogMemory SelectLatestMemory(params MemoryLoader[] memoryLoaders)
        {
            CharaDialogMemory latest = null;
            SaveData.CharaData charaData = null;
            var currentGuid = Guid.Empty;
            ChaFileControl.SafeProc(cfc => cfc.GetCharaData().SafeProc(cd =>
            {
                charaData = cd;
                currentGuid = cd.GetCharaGuid();
            }));
            foreach (var loader in memoryLoaders)
            {
                var memory = loader(charaData, currentGuid, latest?.LastUpdated ?? 0);
                if (memory == null) continue;
                if (latest == memory) continue;
                memory = MemoryLoaderValidation(loader.Method.Name, memory);
                if (memory == null) continue;
                if (latest == null || (latest != memory && latest.LastUpdated < memory.LastUpdated))
                {
                    latest = memory;
                }
            }

            return latest;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PersistToCard();
        }

        [UsedImplicitly]
        private CharaDialogMemory CopyDialogMemory()
        {
            // ReSharper disable once MergeConditionalExpression - Unity
            var clone = new CharaDialogMemory(ChaFileControl == null ? null : ChaFileControl.GetCharaData());
            if (_dialogMemory == null) return clone;

            foreach (var questionEntry in _dialogMemory)
            {
                foreach (var answerEntry in questionEntry.Value)
                {
                    answerEntry.Value.CopyTo(clone[questionEntry.Key][answerEntry.Key]);
                }
            }

            return clone;
        }

        private CharaDialogMemory MemoryLoaderValidation(string callerName, CharaDialogMemory memory,
            Func<CharaDialogMemory, bool> preCheck = null)
        {
            var prefix = $"{callerName,30} {GetLogId(),30}";
            if (memory == null)
            {
                Logger?.DebugLogDebug($"{prefix}: no data");
                return null;
            }

            if (preCheck != null && !preCheck.Invoke(memory))
            {
                Logger?.DebugLogDebug($"{prefix}: pre-check failed, discarding");
                return null;
            }

            if (!memory.IsValidForCurrentSession(prefix))
            {
                Logger?.DebugLogDebug($"{prefix}: invalid data, discarding");
                return null;
            }

            SaveData.Heroine heroine = null;
            ChaFileControl.SafeProc(cfc => cfc.GetHeroine().SafeProc(h => heroine = h));
            if (heroine != null && heroine.relation < 0 && memory.QuestionsAnswered > 0)
            {
                Logger.DebugLogDebug($"{prefix}: never talked with heroine, discarding");
                return null;
            }

            Logger.DebugLogDebug($"{prefix}: valid data found: {memory.LastUpdated}");
            return memory;
        }

        private CharaDialogMemory GetCurrentMemory(SaveData.CharaData _1, Guid _2, long _3)
        {
            ValidateOrClearDialogMemory();
            return _dialogMemory;
        }

        protected internal CharaDialogMemory LoadMemoryFromCard(SaveData.CharaData charaData, Guid currentGuid,
            long lastUpdated = 0)
        {
            if (charaData == null) return null;
            var cardMem = LoadMemoryFromCardInternal(lastUpdated);
            if (cardMem == null) return null;

            var checkGuid = cardMem.CharaGuidVersion == PluginDataInfo.CurrentCharaGuidVersion
                ? currentGuid
                : charaData.GetCharaGuid(cardMem.CharaGuidVersion);
            Logger.DebugLogDebug(
                $"{nameof(LoadMemoryFromCard)}: heroine<{cardMem.CharaGuidVersion}, {checkGuid}> <=> cardMem<{cardMem.CharaGuidVersion}, {cardMem.CharaGuid}>");


            // verify memory belongs to this character
            // ReSharper disable once InvertIf - in case translation/upgrade code add here
            if (checkGuid != cardMem.CharaGuid)
            {
                Logger?.LogDebug(
                    $"{nameof(LoadMemoryFromCard)}: {charaData.Name}: data on card does not seem to belong to this character, ignoring");
                return null;
            }

            if (cardMem.CharaGuidVersion != PluginDataInfo.CurrentSaveGuidVersion)
            {
                HandleGuidMigration();
            }

            return cardMem;
        }

        protected internal CharaDialogMemory LoadMemoryFromCardInternal(long lastUpdated = 0)
        {
            CharaDialogMemory cardMem = null;
            var pluginData = GetExtendedData(true);
            // ReSharper disable once ExpressionIsAlwaysNull - in case order changes
            if (pluginData == null) return cardMem;
            if (pluginData.version < PluginDataInfo.MinimumSupportedCardDataVersion)
            {
                Logger?.LogWarning(
                    $"{nameof(LoadMemoryFromCardInternal)}: Discarding unsupported card data (version {pluginData.version})");
            }
            else
            {
                // old saves may not have LastUpdated, use as shortcut if available
                if (lastUpdated > 0 &&
                    pluginData.data.TryGetValue(PluginDataInfo.Keys.LastUpdated, out var tmpLastUpdated) &&
                    tmpLastUpdated is long cardLastUpdated &&
                    cardLastUpdated <= lastUpdated)
                {
                    return null;
                }

                // ReSharper disable once ExpressionIsAlwaysNull - in case order changes
                if (!pluginData.data.TryGetValue(PluginDataInfo.Keys.DialogMemory, out var val)) return cardMem;
                var heroineGuid = Guid.Empty;
                var heroineGuidVersion = -1;
                Logger?.DebugLogDebug($"{nameof(LoadMemoryFromCardInternal)}: data found on card");
                if (GameDialogHelper.Instance.CurrentSaveGuid != Guid.Empty)
                {
                    if (pluginData.data.TryGetValue(PluginDataInfo.Keys.SaveGuid, out var saveGuidVal))
                    {
                        Guid saveGuid;
                        try
                        {
                            saveGuid = new Guid((byte[])saveGuidVal);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (ArgumentException)
                        {
                            saveGuid = Guid.Empty;
                        }
#pragma warning restore CA1031 // Do not catch general exception types

                        if (saveGuid != Guid.Empty && saveGuid != GameDialogHelper.Instance.CurrentSaveGuid)
                        {
                            Logger?.DebugLogDebug(
                                "data on card does not seem to belong to this save game, ignoring");
                            return null;
                        }
                    }

                    if (pluginData.data.TryGetValue(PluginDataInfo.Keys.CharaGuidVersion, out var tmpCharaGuidVersion))
                    {
                        try
                        {
                            heroineGuidVersion = (int)tmpCharaGuidVersion;
                        }
#pragma warning disable CA1031
                        catch
                        {
                            heroineGuidVersion = -1;
                        }
#pragma warning restore CA1031
                    }

                    if (pluginData.data.TryGetValue(PluginDataInfo.Keys.CharaGuid, out var heroineGuidVal))
                    {
                        try
                        {
                            heroineGuid = new Guid((byte[])heroineGuidVal);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (ArgumentException)
                        {
                            heroineGuid = Guid.Empty;
                        }
#pragma warning restore CA1031 // Do not catch general exception types
                    }
                }

                cardMem = MessagePackSerializer.Deserialize<CharaDialogMemory>((byte[])val);

                if (cardMem == null) return null;

                if (!cardMem.CharaGuid.Equals(Guid.Empty) && cardMem.CharaGuidVersion == -1)
                {
                    ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(h =>
                    {
                        for (var i = 1; i <= PluginDataInfo.MaxCharaGuidVersion; i++)
                        {
                            Guid tmpGuid;
                            try
                            {
                                tmpGuid = h.GetCharaGuid(i);
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                            catch (ArgumentOutOfRangeException)
                            {
                                continue;
                            }
#pragma warning restore CA1031 // Do not catch general exception types

                            if (tmpGuid != cardMem.CharaGuid) continue;
                            Logger?.DebugLogDebug($"{nameof(LoadMemoryFromCardInternal)}: Setting Guid version to {i}");
                            cardMem.CharaGuidVersion = i;
                            if (heroineGuidVersion == -1) heroineGuidVersion = i;
                            break;
                        }
                    }));
                }

                if (heroineGuid == cardMem.CharaGuid || heroineGuid == Guid.Empty ||
                    cardMem.CharaGuid == Guid.Empty)
                {
                    return cardMem;
                }

                if (AreCharaGuidsEqual(heroineGuid, cardMem.CharaGuid))
                {
                    Logger?.DebugLogDebug($"{nameof(LoadMemoryFromCardInternal)}: GUID remap detected, keeping data");
                }
                else
                {
                    Logger?.LogDebug(
                        $"{nameof(LoadMemoryFromCardInternal)}: data on card does not seem to belong to this character, ignoring");
                    return null;
                }
            }

            return cardMem;
        }

        public static bool GuidRemapsTo(Guid check, Guid target)
        {
            if (check == target) return true;
            var currentCheck = check;
            while (CharaGuidMap.TryGetValue(currentCheck, out var result))
            {
                if (result.Equals(target)) return true;
                currentCheck = result;
            }

            return false;
        }

        public static bool AreCharaGuidsEqual(Guid guid1, Guid guid2)
        {
            return guid1 == guid2 || GuidRemapsTo(guid1, guid2) || GuidRemapsTo(guid2, guid1);
        }

        /*
        internal bool IsValidGuidForChara(Guid check)
        {
            return _previousGuids.Contains(check) || AreCharaGuidsEqual(check,
                ChaControl.GetCharaData().GetCharaGuid());
        }
        */

        internal void ProcessGuidChange(Guid newGuid)
        {
            var currentGuid = DialogMemory.CharaGuid;
            if (newGuid.Equals(currentGuid)) return;
            Logger.DebugLogDebug(
                $"{nameof(ProcessGuidChange)}: handling GUID change from {currentGuid} => {newGuid}");
            if (!currentGuid.Equals(Guid.Empty)) _previousGuids.Add(currentGuid);
            DialogMemory.CharaGuid = newGuid;
        }

        private CharaDialogMemory LoadPersistentMemoryByGuid(SaveData.CharaData charaData, Guid currentGuid, long _)
        {
            if (charaData == null) return MemoryLoaderValidation(nameof(LoadPersistentMemoryByGuid), null);
            if (currentGuid == Guid.Empty) return MemoryLoaderValidation(nameof(LoadPersistentMemoryByGuid), null);
            PersistentMemoryByGuid.TryGetValue(currentGuid, out var persistMem);
            return persistMem;
        }

        /*
        private CharaDialogMemory LoadPersistentMemoryByHeroine(SaveData.CharaData charaData, Guid _1, long _2)
        {

            return charaData is SaveData.Heroine heroine &&
                   PersistentMemoryByHeroine.TryGetValue(heroine, out var persistMem)
                ? persistMem
                : null;
        }
        */

        private void PersistToMemory()
        {
            if (_dialogMemory == null) return;
            // If DialogMemory was initialized before current SaveGuid, update it
            if (_dialogMemory.SaveGuid == Guid.Empty &&
                _dialogMemory.SessionGuid == GameDialogHelper.Instance.CurrentSessionGuid)
            {
                _dialogMemory.SaveGuid = GameDialogHelper.Instance.CurrentSaveGuid;
                _dialogMemory.SaveGuidVersion = PluginDataInfo.CurrentSaveGuidVersion;
            }

            Assert.AreNotEqual(_dialogMemory.SaveGuid, Guid.Empty,
                $"{nameof(PersistToMemory)}: {nameof(_dialogMemory.SaveGuid)} should not be empty");
            Assert.AreNotEqual(_dialogMemory.PlayerGuid, Guid.Empty,
                $"{nameof(PersistToMemory)}: {nameof(_dialogMemory.PlayerGuid)} should not be empty");
            Assert.AreNotEqual(_dialogMemory.CharaGuid, Guid.Empty,
                $"{nameof(PersistToMemory)}: {nameof(_dialogMemory.CharaGuid)} should not be empty");

            void Store(SaveData.CharaData charaData)
            {
                if (!(charaData is SaveData.Heroine heroine)) return;
                PersistentMemoryByHeroine[heroine] = _dialogMemory;

                var guid = heroine.GetCharaGuid();
                if (guid != Guid.Empty) PersistentMemoryByGuid[guid] = _dialogMemory;
            }

            ChaFileControl.SafeProc(c => c.GetCharaData().SafeProc(Store));
        }

        private void HandleGuidMigration()
        {
            ChaFileControl.SafeProc(cfc => cfc.GetCharaData().SafeProc(cd =>
            {
                var previousGuid = Guid.Empty;

                for (var ver = PluginDataInfo.MinimumSupportedCharaGuidVersion;
                    ver <= PluginDataInfo.MaxCharaGuidVersion;
                    ver++)
                {
                    var nextGuid = cd.GetCharaGuid(ver);
                    if (previousGuid != Guid.Empty)
                    {
                        _previousGuids.Add(previousGuid);
                        CharaGuidMap[previousGuid] = nextGuid;
                    }

                    previousGuid = nextGuid;
                }
            }));
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (ChaControl.sex == (byte)CharacterSex.Male)
            {
                HandleGuidMigration();
                Assert.AreEqual(GameDialogHelper.Instance.CurrentPlayerGuid, ChaControl.GetCharaData().GetCharaGuid(),
                    $"{nameof(OnReload)}: {nameof(GameDialogHelper.CurrentPlayerGuid)}: should equal current player");
                return;
            }

            _heroineIndex = -1;

            ValidateOrClearDialogMemory();

            var latest = SelectLatestMemory(
                GetCurrentMemory,
                LoadMemoryFromCard,
                LoadPersistentMemoryByGuid /*,
                LoadPersistentMemoryByHeroine*/);
            _dialogMemory = latest;
            if (_dialogMemory == null) return;

            _dialogMemoryInitialized = true;

            // Don't access via DialogMemory
            var outdatedCharaGuidVersion = _dialogMemory.CharaGuidVersion < PluginDataInfo.CurrentCharaGuidVersion;
            if (outdatedCharaGuidVersion &&
                _dialogMemory.CharaGuid.Equals(Guid.Empty))
            {
                ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(h =>
                {
                    var currentGuid = h.GetCharaGuid();
                    Logger?.DebugLogDebug(
                        $"Upgrading GUID version {_dialogMemory.CharaGuidVersion}:{_dialogMemory.CharaGuid} => {PluginDataInfo.CurrentCharaGuidVersion}:{currentGuid}");
                    ProcessGuidChange(currentGuid);
                    _dialogMemory.CharaGuidVersion = PluginDataInfo.CurrentCharaGuidVersion;
                }));
            }

            PersistToMemory();
            Dump();
        }

        public void Remember(int question, int answer, bool isCorrect)
        {
            DialogMemory[question][answer].Remember(isCorrect);
        }


        [Conditional("DEBUG")]
        [UsedImplicitly]
        internal void Dump()
        {
            if (!_dialogMemoryInitialized) return;
            var questions = DialogMemory.Keys.ToList();
            questions.Sort();
            var output = StringBuilderPool.Get();
            try
            {
                foreach (var qid in questions)
                {
                    output.AppendFormat("{0:d3}: ", qid);
                    var maxAnswer = DialogMemory[qid].Keys.ToList().Max();
                    for (var i = 0; i <= maxAnswer; i++)
                    {
                        ulong timesAnswered = 0;
                        var recall = 0f;
                        if (DialogMemory[qid].TryGetValue(i, out var answerMemory))
                        {
                            timesAnswered = answerMemory.TimesAnswered;
                            recall = answerMemory.Recall;
                        }

                        output.AppendFormat("{0:d4}", timesAnswered)
                            .Append('/')
                            .AppendFormat("{0:000.00%}", recall)
                            .Append(' ');
                    }

                    output.Append('\n');
                }

                output.Append("Last Updated: ").Append(DialogMemory.LastUpdated).Append('\n');

                var dump = output.ToString();

                Logger?.LogDebug($"DialogMemory dump {GetLogId()}:\n{dump}");
            }
            finally
            {
                StringBuilderPool.Release(output);
            }
        }

        public ulong TimesAnswerSelected(int question, int answer)
        {
            return DialogMemory[question][answer].TimesAnswered;
        }

        public ulong TimesQuestionAnswered(int question)
        {
            return DialogMemory[question].TimesAnswered;
        }

        public bool CanRecallAnswer(int question, int answer)
        {
            return DialogMemory.TryGetValue(question, out var questionMemory) &&
                   questionMemory.TryGetValue(answer, out var answerMemory) && answerMemory.TimesAnswered > 0;
        }

        public bool CanRecallQuestion(int question)
        {
            return DialogMemory.TryGetValue(question, out var questionMemory) && questionMemory.TimesAnswered > 0;
        }

        public float CurrentRecallChance(int question, int answer)
        {
            if (DialogMemory.TryGetValue(question, out var questionMemory) &&
                questionMemory.TryGetValue(answer, out var answerMemory))
            {
                return answerMemory.Recall;
            }

            return 0f;
        }


        public void PersistToCard()
        {
            if (ChaControl == null)
            {
                Logger?.LogInfo($"ChaControl is null, unable to persist data: {GetLogId()}");
                return;
            }

            var charaData = ChaControl.GetCharaData();
            if (charaData == null) return;

            var charaGuid = charaData.GetCharaGuid();
            Logger?.DebugLogDebug($"{nameof(PersistToCard)}: {GetLogId()}: {charaData}: {charaGuid}");

            var pluginData = new PluginData {version = PluginDataInfo.DataVersion};
            pluginData.data.Add(PluginDataInfo.Keys.SaveGuid, GameDialogHelper.Instance.CurrentSaveGuid.ToByteArray());
            pluginData.data.Add(PluginDataInfo.Keys.SaveGuidVersion, PluginDataInfo.CurrentSaveGuidVersion);

            if (DialogMemory.CharaGuid == Guid.Empty || DialogMemory.CharaGuid != charaGuid ||
                DialogMemory.CharaGuidVersion != PluginDataInfo.CurrentCharaGuidVersion)
            {
                ProcessGuidChange(charaGuid);
                DialogMemory.CharaGuid = charaGuid;
                DialogMemory.CharaGuidVersion = PluginDataInfo.CurrentCharaGuidVersion;
                DialogMemory.LastUpdated = DateTime.UtcNow.Ticks;
            }

            pluginData.data.Add(PluginDataInfo.Keys.CharaGuidVersion, PluginDataInfo.CurrentCharaGuidVersion);
            pluginData.data.Add(PluginDataInfo.Keys.PlayerGuid,
                GameDialogHelper.Instance.CurrentPlayerGuid.ToByteArray());
            pluginData.data.Add(PluginDataInfo.Keys.CharaGuid, charaGuid.ToByteArray());
            pluginData.data.Add(PluginDataInfo.Keys.LastUpdated, DialogMemory.LastUpdated);

            if (charaData is SaveData.Player)
            {
                SetExtendedData(pluginData);
                LastPersistedToCard = DateTime.UtcNow.Ticks;
                Logger.DebugLogDebug($"PersistToCard {GetLogId()} done");
                return;
            }


            if (!_dialogMemoryInitialized) return;

            if (!(charaData is SaveData.Heroine))
            {
                Logger?.LogInfo($"heroine is null, unable to persist data: {GetLogId()}");
                return;
            }

            PersistToMemory();

            if (LastPersistedToCard > DialogMemory.LastUpdated &&
                DialogMemory.SaveGuid == GameDialogHelper.Instance.CurrentSaveGuid)
            {
                return;
            }

            if (DialogMemory.Count > 0)
            {
                DialogMemory.PlayerGuid = GameDialogHelper.Instance.CurrentPlayerGuid;
                DialogMemory.SaveGuid = GameDialogHelper.Instance.CurrentSaveGuid;
                DialogMemory.SaveGuidVersion = PluginDataInfo.CurrentSaveGuidVersion;

                DialogMemory.LastUpdated = DateTime.UtcNow.Ticks;
                pluginData.data.Add(PluginDataInfo.Keys.DialogMemory, MessagePackSerializer.Serialize(DialogMemory));
            }

            SetExtendedData(pluginData);
            LastPersistedToCard = DateTime.UtcNow.Ticks;

            Logger.DebugLogDebug($"PersistToCard {GetLogId()} done");
            Dump();
        }

        private string GetLogId()
        {
            if (ChaFileControl == null) return " ??? ";
            if (ChaControl.sex == 0)
            {
                var player = ChaFileControl.GetPlayer();
                return player == null ? "P ? ?" : $"{player.GetHashCode()}:{player.Name}";
            }

            var heroine = ChaFileControl.GetHeroine();
            return heroine == null ? "? ? ?" : $"{heroine.GetHashCode()}:{heroine.Name}:{HeroineIndex}";
        }

        public static void DoReset()
        {
            PersistentMemoryByHeroine.Clear();
            PersistentMemoryByGuid.Clear();
            CharaGuidMap.Clear();
            foreach (var behaviour in CharacterApi.GetBehaviours())
            {
                if (!(behaviour is GameDialogHelperCharaController controller)) continue;

                controller.OnReset();
            }
        }

        private void OnReset()
        {
            _previousGuids.Clear();
        }

        private delegate CharaDialogMemory MemoryLoader(SaveData.CharaData charaData, Guid currentGuid,
            long lastUpdated);
    }
}
