using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GameDialogHelperPlugin.AdvancedLogicMemory;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using Manager;
using MessagePack;
using UnityEngine;

namespace GameDialogHelperPlugin
{
    public class GameDialogHelperCharaController : CharaCustomFunctionController
    {
        private static ManualLogSource Logger => GameDialogHelper.Logger;

        internal static readonly Dictionary<Guid, CharaDialogMemory>
            PersistentMemoryByGuid = new Dictionary<Guid, CharaDialogMemory>();

        internal static readonly Dictionary<SaveData.Heroine, CharaDialogMemory>
            PersistentMemoryByHeroine = new Dictionary<SaveData.Heroine, CharaDialogMemory>();

        

        internal static readonly Dictionary<Guid, Guid> HeroineGuidMap = new Dictionary<Guid, Guid>();

        private readonly HashSet<Guid> _previousGuids = new HashSet<Guid>();

        private CharaDialogMemory _dialogMemory;
        private bool _dialogMemoryInitialized;

        private int _heroineIndex = -1;

        private long _lastPersistedToCard = -1;

        public long LastPersistedToCard
        {
            get => _lastPersistedToCard;
            internal set => _lastPersistedToCard = Math.Max(value, _lastPersistedToCard);
        }

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
                        HeroineGuidVersion = GameDialogHelper.CurrentHeroineGuidVersion
                    };
                    ChaFileControl.SafeProc(
                        cfc => cfc.GetHeroine().SafeProc(h => result.HeroineGuid = h.GetHeroineGuid()));
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

        private delegate CharaDialogMemory MemoryLoader();

        private CharaDialogMemory SelectLatestMemory(params MemoryLoader[] memoryLoaders)
        {
            CharaDialogMemory latest = null;
            foreach (var loader in memoryLoaders)
            {
                var memory = loader();
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

        /*
        private IEnumerator PersistDialogMemoryCoroutine(ChaFileControl chaFileControl, Dictionary<int, Dictionary<int, ulong>> value)
        {
            SaveData.Heroine heroine = chaFileControl.GetHeroine();
            while (heroine == null)
            {
                Logger.LogDebug("PersistDialogMemoryCoroutine: no heroine");
                yield return null;
                heroine = chaFileControl.GetHeroine();
            }

            Logger.LogDebug("PersistDialogMemoryCoroutine: heroine available");
            PersistentMemoryByHeroine[heroine] = value;
            GameDialogHelper.LoadedFromCard.Add(heroine);
            PersistToCard();
        }
        */

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PersistToCard();
        }

        [UsedImplicitly]
        private CharaDialogMemory CopyDialogMemory()
        {
            // ReSharper disable once MergeConditionalExpression - Unity
            var clone = new CharaDialogMemory(ChaFileControl == null ? null : ChaFileControl.GetHeroine());
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
                Logger.LogDebug($"{prefix}: no data");
                return null;
            }

            if (preCheck != null && !preCheck.Invoke(memory))
            {
                Logger.LogDebug($"{prefix}: pre-check failed, discarding");
                return null;
            }

            if (!memory.IsValidForCurrentSession(prefix))
            {
                Logger.LogDebug($"{prefix}: invalid data, discarding");
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

        private CharaDialogMemory GetCurrentMemory()
        {
            ValidateOrClearDialogMemory();
            return _dialogMemory;
        }

        protected internal CharaDialogMemory LoadMemoryFromCard()
        {
            SaveData.Heroine heroine = null;
            ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(h => heroine = h));
            if (heroine == null) return null;
            var cardMem = LoadMemoryFromCardInternal();
            if (cardMem == null) return null;

            var checkGuid = heroine.GetHeroineGuid(cardMem.HeroineGuidVersion);
            Logger.DebugLogDebug($"{nameof(LoadMemoryFromCard)}: heroine<{cardMem.HeroineGuidVersion}, {checkGuid}> <=> cardMem<{cardMem.HeroineGuidVersion}, {cardMem.HeroineGuid}>");

            
            // verify memory belongs to this character
            if (checkGuid != cardMem.HeroineGuid)
            {
                Logger.LogDebug(
                    $"{nameof(LoadMemoryFromCard)}: {heroine.Name}: data on card does not seem to belong to this character, ignoring");
                return null;
            }

            /*
            // if guid was missing or outdated, upgrade to current version
            if (cardMem.HeroineGuid.Equals(Guid.Empty))
            {
                cardMem.HeroineGuid = heroine.GetHeroineGuid();
                cardMem.HeroineGuidVersion = GameDialogHelper.CurrentHeroineGuidVersion;
                Logger.DebugLogDebug(
                    $"{nameof(LoadMemoryFromCard)}: updated Guid: cardMem<{cardMem.HeroineGuidVersion}, {cardMem.HeroineGuid}>");
            }
            else if (cardMem.HeroineGuid != heroine.GetHeroineGuid())
            {
                Logger.LogDebug(
                    $"{nameof(LoadMemoryFromCard)}: {heroine.Name}: data on card does not seem to belong to this character, ignoring");
                return null;
            }
            */
            return cardMem;
        }

        protected internal CharaDialogMemory LoadMemoryFromCardInternal()
        {
            CharaDialogMemory cardMem = null;
            var pluginData = GetExtendedData(true);
            // ReSharper disable once ExpressionIsAlwaysNull - in case order changes
            if (pluginData == null) return cardMem;
            if (pluginData.version < PluginDataInfo.MinimumSupportedDataVersion)
            {
                Logger.LogWarning(
                    $"Discarding unsupported card data (version {pluginData.version})");
            }
            else
            {
                // ReSharper disable once ExpressionIsAlwaysNull - in case order changes
                if (!pluginData.data.TryGetValue(PluginDataInfo.Keys.DialogMemory, out var val)) return cardMem;
                var heroineGuid = Guid.Empty;
                int heroineGuidVersion = -1;
                Logger.LogDebug("data found on card");
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
                            Logger.LogDebug(
                                "data on card does not seem to belong to this save game, ignoring");
                            return null;
                        }
                    }

                    if (!pluginData.data.TryGetValue(PluginDataInfo.Keys.HeroineGuidVersion, out var tmpHeroineGuidVersion))
                    {
                        try
                        {
                            heroineGuidVersion = (int)tmpHeroineGuidVersion;
                        }
#pragma warning disable CA1031
                        catch
                        {
                            heroineGuidVersion = -1;
                        }
#pragma warning restore CA1031

                    }

                    if (pluginData.data.TryGetValue(PluginDataInfo.Keys.HeroineGuid, out var heroineGuidVal))
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
                
                if (!cardMem.HeroineGuid.Equals(Guid.Empty) && cardMem.HeroineGuidVersion == -1)
                {
                    ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(h =>
                    {
                        for (var i = 1; i <= GameDialogHelper.MaxHeroineGuidVersion; i++)
                        {
                            var tmpGuid = h.GetHeroineGuid(i);
                            if (tmpGuid != cardMem.HeroineGuid) continue;
                            Logger.LogDebug($"{nameof(LoadMemoryFromCardInternal)}: Setting Guid version to {i}");
                            cardMem.HeroineGuidVersion = i;
                            if (heroineGuidVersion == -1) heroineGuidVersion = i;
                            break;
                        }
                    }));
                }

                if (heroineGuid.Equals(cardMem.HeroineGuid) || heroineGuid.Equals(Guid.Empty) ||
                    cardMem.HeroineGuid.Equals(Guid.Empty))
                {
                    return cardMem;
                }

                if (GuidRemapsTo(heroineGuid, cardMem.HeroineGuid) || GuidRemapsTo(cardMem.HeroineGuid, heroineGuid))
                {
                    Logger.LogDebug($"{nameof(LoadMemoryFromCardInternal)}: GUID remap detected, keeping data");
                }
                else
                {
                    Logger.LogDebug(
                        $"{nameof(LoadMemoryFromCardInternal)}: data on card does not seem to belong to this character, ignoring");
                    return null;
                }
            }
            

            return cardMem;
        }

        internal bool GuidRemapsTo(Guid check, Guid target)
        {
            if (check.Equals(target) || _previousGuids.Contains(check)) return true;
            var currentCheck = check;
            while (HeroineGuidMap.TryGetValue(currentCheck, out var result))
            {
                if (result.Equals(target)) return true;
                currentCheck = result;
            }

            return false;
        }

        internal void ProcessGuidChange(Guid newGuid)
        {
            var currentGuid = DialogMemory.HeroineGuid;
            if (newGuid.Equals(currentGuid)) return;
            Logger.DebugLogDebug(
                $"{nameof(ProcessGuidChange)}: handling GUID change from {currentGuid} => {newGuid}");
            if (!currentGuid.Equals(Guid.Empty)) _previousGuids.Add(currentGuid);
            DialogMemory.HeroineGuid = newGuid;
        }

        private CharaDialogMemory LoadPersistentMemoryByGuid()
        {
            SaveData.Heroine heroine = null;
            ChaFileControl.SafeProc(cfc => heroine = cfc.GetHeroine());
            if (heroine == null) return MemoryLoaderValidation(nameof(LoadPersistentMemoryByGuid), null);
            var guid = heroine.GetHeroineGuid();
            if (guid == Guid.Empty) return MemoryLoaderValidation(nameof(LoadPersistentMemoryByGuid), null);

            PersistentMemoryByGuid.TryGetValue(guid, out var persistMem);
            return persistMem;
        }

#if DEADCODE
        private CharaDialogMemory LoadPersistentMemoryByHeroine()
        {
            SaveData.Heroine heroine = null;
            ChaFileControl.SafeProc(cfc => heroine = cfc.GetHeroine());
            if (heroine == null) return null;

            PersistentMemoryByHeroine.TryGetValue(heroine, out var persistMem);
            return persistMem;
        }
#endif

        private void PersistToMemory()
        {
            if (_dialogMemory == null) return;

            void Store(SaveData.Heroine heroine)
            {
                PersistentMemoryByHeroine[heroine] = _dialogMemory;

                var guid = heroine.GetHeroineGuid();
                if (guid != Guid.Empty) PersistentMemoryByGuid[guid] = _dialogMemory;
            }

            ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(Store));
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (ChaControl.sex == 0)
            {
                if (GameDialogHelper.Instance.CurrentSaveGuid == Guid.Empty) UpdateSaveGuidFromPlayer();
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
            if (_dialogMemory.HeroineGuidVersion < GameDialogHelper.CurrentHeroineGuidVersion &&
                _dialogMemory.HeroineGuid.Equals(Guid.Empty))
            {
                ChaFileControl.SafeProc(c => c.GetHeroine().SafeProc(h =>
                {
                    var currentGuid = h.GetHeroineGuid();
                    Logger.LogDebug(
                        $"Upgrading GUID version {_dialogMemory.HeroineGuidVersion}:{_dialogMemory.HeroineGuid} => {GameDialogHelper.CurrentHeroineGuidVersion}:{currentGuid}");
                    ProcessGuidChange(currentGuid);
                    _dialogMemory.HeroineGuidVersion = GameDialogHelper.CurrentHeroineGuidVersion;
                }));
            }

            PersistToMemory();
        }

        internal void UpdateSaveGuidFromPlayer()
        {
            //Logger.LogDebug($"{nameof(UpdateSaveGuidFromPlayer)}: called");
            GameDialogHelperCharaController playerController = null;
            Game.Instance.SafeProc(g =>
                g.Player.SafeProc(p => p.chaCtrl.SafeProc(c => playerController = c.GetGameDialogHelperController())));

            if (this != playerController) return;
            //Logger.LogDebug($"{nameof(UpdateSaveGuidFromPlayer)}: current is player");

            var pluginData = GetExtendedData();
            if (pluginData == null) return;

            //Logger.LogDebug($"{nameof(UpdateSaveGuidFromPlayer)}: plugin data found");
            if (pluginData.version < PluginDataInfo.MinimumSupportedDataVersion) return;

            //Logger.LogDebug($"{nameof(UpdateSaveGuidFromPlayer)}: plugin data supported");
            if (!pluginData.data.TryGetValue(PluginDataInfo.Keys.SaveGuid, out var val)) return;

            Logger.LogDebug(
                $"{nameof(UpdateSaveGuidFromPlayer)}: save guid in plugin data");
            var saveGuid = new Guid((byte[])val);
            Logger.LogDebug($"{nameof(UpdateSaveGuidFromPlayer)}: {saveGuid}");
            GameDialogHelper.Instance.CurrentSaveGuid = saveGuid;
        }

#if DEADCODE
        private void SceneUnloaded(Scene arg0)
        {
            if (!_dialogMemoryInitialized) return;
            Logger.LogDebug($"SceneUnloaded({arg0.name}): {GetLogId()}");
            PersistToCard();
            ChaFileControl.SafeProc(c =>
                c.GetHeroine().SafeProc(h => StartCoroutine(ApplyDataAfterRefresh(h))));
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (!_dialogMemoryInitialized) return;
            Logger.LogDebug($"SceneLoaded({arg0.name},{arg1}): {GetLogId()}");
            PersistToCard();
            /*
            ChaFileControl.SafeProc(c =>
                c.GetHeroine().SafeProc(h => StartCoroutine(ApplyDataAfterRefresh(h))));
            */
        }

        internal void PersistDataAndApply()
        {
            PersistToCard();
            ChaFileControl.SafeProc(c =>
                c.GetHeroine().SafeProc(h => StartCoroutine(ApplyDataAfterRefresh(h))));
        }

        private IEnumerator ApplyDataAfterRefresh(SaveData.Heroine heroine)
        {
            if (heroine.chaCtrl == null || _dialogMemory == null || _dialogMemory.Count == 0)
            {
                yield break;
            }

            var previousControl = heroine.chaCtrl;
            // backup memory 
            var previousMemory = CopyDialogMemory();
            // Wait until we switch from temporary character copy to the character used in the next scene
            yield return new WaitUntil(() => heroine.chaCtrl != previousControl && heroine.chaCtrl != null);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();


            // Apply the backup memory to new controller
            heroine.GetGameDialogHelperController().SafeProcObject(ctrl => ctrl.DialogMemory = previousMemory);
            Logger.LogDebug($"Applying PersistToCard {GetLogId()} done");
        }
#endif

        public void Remember(int question, int answer, bool isCorrect)
        {
            DialogMemory[question][answer].Remember(isCorrect);
        }


        [Conditional("DEBUG")]
        internal void Dump()
        {
            if (!_dialogMemoryInitialized) return;
            var questions = DialogMemory.Keys.ToList();
            questions.Sort();
            var output = new StringBuilder();
            foreach (var qid in questions)
            {
                output.Append($"{qid:d3}: ");
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

                    output.Append($"{timesAnswered:d4}/{recall:000.00%} ");
                }
                /*
                var answers = DialogMemory[qid].Keys.ToList();
                answers.Sort();
                foreach (var answer in answers)
                {
                    output.Append($"{answer:d}({DialogMemory[qid][answer]:d4}) ");
                }
                */

                output.Append("\n");
            }

            output.Append("Last Updated: ").Append(DialogMemory.LastUpdated).Append("\n");

            var dump = output.ToString();

            Logger.LogDebug($"DialogMemory dump {GetLogId()}:\n{dump}");
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
                Logger.LogWarning($"ChaControl is null, unable to persist data: {GetLogId()}");
                return;
            }

            var pluginData = new PluginData {version = PluginDataInfo.DataVersion};
            pluginData.data.Add(PluginDataInfo.Keys.SaveGuid, GameDialogHelper.Instance.CurrentSaveGuid.ToByteArray());

            // player
            if (ChaControl.sex == 0)
            {
                SetExtendedData(pluginData);
                LastPersistedToCard = DateTime.UtcNow.Ticks;
                Logger.DebugLogDebug($"PersistToCard {GetLogId()} done");
                return;
            }

            
            if (!_dialogMemoryInitialized) return;
            
            // heroines
            var heroine = ChaControl.GetHeroine();
            if (heroine == null)
            {
                Logger.LogWarning($"heroine is null, unable to persist data: {GetLogId()}");
                return;
            } 
 
            PersistToMemory();

            if (LastPersistedToCard > DialogMemory.LastUpdated &&
                DialogMemory.SaveGuid == GameDialogHelper.Instance.CurrentSaveGuid)
            {
                return;
            }

            var currentGuid = heroine.GetHeroineGuid();

            if (DialogMemory.HeroineGuid == Guid.Empty || DialogMemory.HeroineGuid != currentGuid || DialogMemory.HeroineGuidVersion != GameDialogHelper.CurrentHeroineGuidVersion)
            {
                ProcessGuidChange(currentGuid);
                DialogMemory.HeroineGuid = currentGuid;
                DialogMemory.HeroineGuidVersion = GameDialogHelper.CurrentHeroineGuidVersion;
            }

            if (DialogMemory.Count > 0)
            {
                DialogMemory.SaveGuid = GameDialogHelper.Instance.CurrentSaveGuid;
                DialogMemory.LastUpdated = DateTime.UtcNow.Ticks;
                pluginData.data.Add(PluginDataInfo.Keys.DialogMemory, MessagePackSerializer.Serialize(DialogMemory));
            }

            pluginData.data.Add(PluginDataInfo.Keys.HeroineGuid, currentGuid.ToByteArray());
            pluginData.data.Add(PluginDataInfo.Keys.HeroineGuidVersion, GameDialogHelper.CurrentHeroineGuidVersion);
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
            HeroineGuidMap.Clear();
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
    }
}
