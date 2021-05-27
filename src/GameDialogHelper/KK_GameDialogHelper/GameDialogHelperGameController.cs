using System;
using System.Collections;
using System.Collections.Generic;
using ActionGame;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon.Utilities;
using KKAPI.MainGame;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace GameDialogHelperPlugin
{
    internal class GameDialogHelperGameController : GameCustomFunctionController
    {
        private readonly Dictionary<string, Guid> _heroineMap = new Dictionary<string, Guid>();

        private readonly IEnumerator _waitOnGameInstance = new WaitUntil(Game.IsInstance);

        private bool _startingNewGame;

        private static ManualLogSource Logger => GameDialogHelper.Logger;

        public void PersistToGame()
        {
            var pluginData = new PluginData {version = PluginDataInfo.DataVersion};
            pluginData.data.Add(PluginDataInfo.Keys.SaveGuid, GameDialogHelper.Instance.CurrentSaveGuid.ToByteArray());
            pluginData.data.Add(PluginDataInfo.Keys.SaveGuidVersion, PluginDataInfo.CurrentSaveGuidVersion);
            pluginData.data.Add(PluginDataInfo.Keys.CharaGuidVersion, PluginDataInfo.CurrentCharaGuidVersion);

            Game.Instance.SafeProc(g => g.Player.SafeProc(p =>
            {
                pluginData.data.Add(PluginDataInfo.Keys.PlayerGuid, p.GetCharaGuid().ToByteArray());
            }));

            SetExtendedData(pluginData);
            Logger.DebugLogDebug("PersistToGame done");
        }

        public void LoadFromGame()
        {
            GameDialogHelper.Instance.CurrentSaveGuid = Guid.Empty;
            var pluginData = GetExtendedData();
            if (pluginData == null || pluginData.version < PluginDataInfo.MinimumSupportedGameDataVersion) return;
            if (!pluginData.data.TryGetValue(PluginDataInfo.Keys.SaveGuid, out var val)) return;
            if (pluginData.data.TryGetValue(PluginDataInfo.Keys.SaveGuidVersion, out var versionData) &&
                versionData is int guidVersion &&
                guidVersion > PluginDataInfo.MaxSaveGuidVersion)
            {
                return;
            }

            var saveGuid = new Guid((byte[])val);
            Logger?.DebugLogDebug($"{nameof(LoadFromGame)}: save guid in plugin data: {saveGuid}");

            if (Game.Instance != null && Game.Instance.Player != null &&
                pluginData.data.TryGetValue(PluginDataInfo.Keys.PlayerGuid, out var playerGuidData))
            {
                if (pluginData.data.TryGetValue(PluginDataInfo.Keys.CharaGuidVersion, out var charaVersionData) &&
                    charaVersionData is int charaGuidVersion &&
                    charaGuidVersion >= PluginDataInfo.MinimumSupportedCharaGuidVersion &&
                    charaGuidVersion <= PluginDataInfo.MaxCharaGuidVersion)
                {
                    var savePlayerGuid = new Guid((byte[])playerGuidData);
                    var calculatedPlayerGuid = Game.Instance.Player.GetCharaGuid(charaGuidVersion);
                    if (savePlayerGuid != calculatedPlayerGuid)
                    {
                        Logger?.LogWarning($"{nameof(LoadFromGame)}: player guid mismatch on save data, discarding");
                        GameDialogHelper.Instance.CurrentSaveGuid = Guid.NewGuid();
                        StartCoroutine(SetupNewGamePlayerCoroutine());
                        return;
                    }
                }
            }

            GameDialogHelper.Instance.CurrentSaveGuid = saveGuid;
        }

        protected override void OnNewGame()
        {
            GameDialogHelper.DoReset();
            GameDialogHelper.Instance.CurrentSaveGuid = Guid.NewGuid();
            _startingNewGame = true;
            PersistToGame();
        }

        private IEnumerator SetupNewGamePlayerCoroutine()
        {
            Logger?.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: start");
            var newGameGuid = GameDialogHelper.Instance.CurrentSaveGuid;
            if (!Game.IsInstance()) yield return _waitOnGameInstance;
            Logger?.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: game available");
            GameDialogHelperCharaController controller;

            while ((controller = Game.Instance.Player.GetGameDialogHelperController()) == null)
            {
                yield return null;
            }

            Logger?.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: player controller available");
            controller.SetExtendedData(null);
            GameDialogHelper.Instance.CurrentSaveGuid = newGameGuid;
            controller.PersistToCard();
            Logger?.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: done");
        }

        private IEnumerator SetupNewGameHeroinesCoroutine()
        {
            Logger?.LogDebug($"{nameof(SetupNewGameHeroinesCoroutine)}: start");
            var newGameGuid = GameDialogHelper.Instance.CurrentSaveGuid;
            if (!Game.IsInstance()) yield return _waitOnGameInstance;
            Logger?.LogDebug($"{nameof(SetupNewGameHeroinesCoroutine)}: game available");
            if (!Game.IsInstance() || Game.Instance.HeroineList == null)
            {
                yield return new WaitUntil(() => Game.IsInstance() && Game.Instance.HeroineList != null);
            }

            Logger?.DebugLogDebug(
                $"{nameof(SetupNewGameHeroinesCoroutine)}: heroine list available {Game.Instance.HeroineList.Count}");

            foreach (var heroine in Game.Instance.HeroineList)
            {
                GameDialogHelper.Instance.StartCoroutine(SetupNewGameHeroineCoroutine(newGameGuid, heroine));
            }

            Logger?.DebugLogDebug($"{nameof(SetupNewGameHeroinesCoroutine)}: done");
        }

        private IEnumerator SetupNewGameHeroineCoroutine(Guid newGameGuid, SaveData.Heroine heroine)
        {
            Logger?.DebugLogDebug($"{nameof(SetupNewGameHeroineCoroutine)}: start");
            GameDialogHelperCharaController controller;

            while ((controller = heroine.GetGameDialogHelperController()) == null)
            {
                yield return null;
            }

            Logger?.DebugLogDebug($"{nameof(SetupNewGameHeroineCoroutine)}: controller available");
            controller.SetExtendedData(null);
            GameDialogHelper.Instance.CurrentSaveGuid = newGameGuid;
            controller.PersistToCard();
            Logger?.DebugLogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: done");
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            GameDialogHelper.DoReset();
            LoadFromGame();
        }

        protected override void OnEnterNightMenu()
        {
            if (_startingNewGame)
            {
                _startingNewGame = false;
                GameDialogHelper.Instance.StartCoroutine(SetupNewGameHeroinesCoroutine());
                GameDialogHelper.Instance.StartCoroutine(SetupNewGamePlayerCoroutine());
            }

            SceneManager.sceneUnloaded += SceneUnloadedWhileInNightMenu;

            CheckHeroineMapping();
            PersistAllToCards();
        }

        private void SceneUnloadedWhileInNightMenu(Scene arg0)
        {
            switch (arg0.name)
            {
                case "NightMenu":
                    SceneManager.sceneUnloaded -= SceneUnloadedWhileInNightMenu;
                    break;

                case "ClassRoomSelect":
                case "Load":
                    CheckHeroineMapping();
                    break;
            }
        }

        private void CheckHeroineMapping()
        {
            string MappingKey(SaveData.Heroine heroine)
            {
                return heroine.ChaName.StartsWith("c-")
                    ? heroine.ChaName
                    : StringUtils.JoinStrings("/", heroine.Name, heroine.ChaName);
            }

            Logger?.DebugLogDebug($"{nameof(CheckHeroineMapping)}: start: {_heroineMap.Count}");
            var previousMap = new Dictionary<string, Guid>();
            foreach (var entry in _heroineMap)
            {
                previousMap[entry.Key] = new Guid(entry.Value.ToByteArray());
                Logger?.DebugLogDebug($"{nameof(CheckHeroineMapping)}: saved: {previousMap[entry.Key]}");
            }

            Logger?.DebugLogDebug($"{nameof(CheckHeroineMapping)}: saved: {previousMap.Count}");
            _heroineMap.Clear();
            for (var i = 0; i < Game.Instance.HeroineList.Count; i++)
            {
                var heroine = Game.Instance.HeroineList[i];
                var key = MappingKey(heroine);
                var currentGuid = _heroineMap[key] = heroine.GetCharaGuid();
                var previousFound = true;
                if (!previousMap.TryGetValue(key, out var previousGuid))
                {
                    previousFound = false;
                    previousGuid = Guid.Empty;
                }

                var memoryGuid = Guid.Empty;
                var controller = heroine.GetGameDialogHelperController();
                if (controller != null)
                {
                    var mem = controller.LoadMemoryFromCard(heroine, currentGuid);
                    if (mem != null) memoryGuid = mem.CharaGuid;
                }

                Logger?.DebugLogDebug(
                    $"{nameof(CheckHeroineMapping)}: {i:D5} {currentGuid} {previousGuid} {memoryGuid} {heroine.Name}");

                if (controller != null && currentGuid != memoryGuid)
                {
                    if (memoryGuid == Guid.Empty || memoryGuid == previousGuid)
                    {
                        controller.ProcessGuidChange(currentGuid);
                    }
                    else
                    {
                        Logger?.LogError(
                            $"{nameof(CheckHeroineMapping)}: {i:D5} {heroine.Name}: dialog memory GUID mismatch");
                    }
                }


                if (!previousFound) continue;

                if (previousGuid.Equals(currentGuid) || previousGuid.Equals(Guid.Empty)) continue;

                if (!previousGuid.Equals(Guid.Empty))
                {
                    GameDialogHelperCharaController.CharaGuidMap[previousGuid] = currentGuid;

                    if (controller != null)
                    {
                        controller.ProcessGuidChange(currentGuid);
                        continue;
                    }
                }

                Logger?.LogError(
                    $"{nameof(CheckHeroineMapping)}: {i:D5} {heroine.Name}: previous GUID mismatch");
            }
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            PersistAllToCards();
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            PersistAllToCards();
        }

        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            PersistAllToCards();
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            PersistToGame();
            PersistAllToCards(true);
        }

        private static void PersistAllToCards(bool isSaving = false)
        {
            for (var i = 0; i < Game.Instance.HeroineList.Count; i++)
            {
                GameDialogHelperCharaController controller = null;
                Game.Instance.HeroineList.SafeProc(i,
                    h => controller = h.GetGameDialogHelperController());
                if (controller == null) continue;
                if (!isSaving && controller.DialogMemory.LastUpdated < controller.LastPersistedToCard) continue;
                controller.PersistToCard();
            }

            // don't change CurrentSaveGuid until after all heroine's have been persisted or they'll all be invalidated
            if (isSaving && GameDialogHelper.Instance.CurrentSaveGuid == Guid.Empty)
            {
                GameDialogHelper.Instance.CurrentSaveGuid = Guid.NewGuid();
                Logger?.LogDebug(
                    $"new save guid: {GameDialogHelper.Instance.CurrentSaveGuid}");

                UpdateSaveGuidOnControllers();
            }

            Game.Instance.Player.chaCtrl.GetGameDialogHelperController().SafeProc(c => c.PersistToCard());
        }

        private static void UpdateSaveGuidOnControllers()
        {
            if (GameDialogHelper.Instance.CurrentSaveGuid == Guid.Empty) return;

            void UpdateCharaData(SaveData.CharaData charaData)
            {
                charaData.SafeProc(cd => cd.GetGameDialogHelperController().SafeProc(ctrl => ctrl.DialogMemory.SafeProc(
                    dm =>
                    {
                        if (dm.SaveGuid == GameDialogHelper.Instance.CurrentSaveGuid) return;
                        if (dm.SaveGuid != Guid.Empty) return;
                        dm.SaveGuid = GameDialogHelper.Instance.CurrentSaveGuid;
                        dm.SaveGuidVersion = PluginDataInfo.CurrentSaveGuidVersion;
                        dm.LastUpdated = DateTime.UtcNow.Ticks;
                        Logger?.DebugLogDebug(
                            $"{nameof(UpdateSaveGuidOnControllers)}: {charaData.Name}: updating save guid");
                        ctrl.PersistToCard();
                    })));
            }

            Game.Instance.HeroineList.ForEach(UpdateCharaData);
            Game.Instance.Player.SafeProc(UpdateCharaData);
        }

        public static void DoReset()
        {
            var controller =
                GameAPI.GetRegisteredBehaviour(
                    typeof(GameDialogHelperGameController)) as GameDialogHelperGameController;
            if (controller == null)
            {
                Logger?.LogWarning(
                    $"Unable to find registered {nameof(GameDialogHelperGameController)}, reset aborted");
                return;
            }

            controller.OnReset();
        }

        private void OnReset()
        {
            _startingNewGame = false;
            _heroineMap.Clear();
        }
    }
}
