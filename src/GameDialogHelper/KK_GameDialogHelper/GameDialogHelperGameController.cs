using System;
using System.Collections;
using System.Collections.Generic;
using ActionGame;
using BepInEx.Logging;
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

        protected override void OnNewGame()
        {
            GameDialogHelper.DoReset();
            GameDialogHelper.Instance.CurrentSaveGuid = Guid.NewGuid();
            _startingNewGame = true;
        }


        private IEnumerator SetupNewGamePlayerCoroutine()
        {
            Logger.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: start");
            var newGameGuid = GameDialogHelper.Instance.CurrentSaveGuid;
            if (!Game.IsInstance()) yield return _waitOnGameInstance;
            Logger.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: game available");
            GameDialogHelperCharaController controller;

            while ((controller = Game.Instance.Player.GetGameDialogHelperController()) == null)
            {
                yield return null;
            }

            Logger.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: player controller available");
            controller.SetExtendedData(null);
            GameDialogHelper.Instance.CurrentSaveGuid = newGameGuid;
            controller.PersistToCard();
            Logger.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: done");
        }

        private IEnumerator SetupNewGameHeroinesCoroutine()
        {
            Logger.LogDebug($"{nameof(SetupNewGameHeroinesCoroutine)}: start");
            var newGameGuid = GameDialogHelper.Instance.CurrentSaveGuid;
            if (!Game.IsInstance()) yield return _waitOnGameInstance;
            Logger.LogDebug($"{nameof(SetupNewGameHeroinesCoroutine)}: game available");
            if (!Game.IsInstance() || Game.Instance.HeroineList == null)
            {
                yield return new WaitUntil(() => Game.IsInstance() && Game.Instance.HeroineList != null);
            }

            Logger.LogDebug(
                $"{nameof(SetupNewGameHeroinesCoroutine)}: heroine list available {Game.Instance.HeroineList.Count}");

            foreach (var heroine in Game.Instance.HeroineList)
            {
                GameDialogHelper.Instance.StartCoroutine(SetupNewGameHeroineCoroutine(newGameGuid, heroine));
            }

            Logger.LogDebug($"{nameof(SetupNewGameHeroinesCoroutine)}: done");
        }

        private IEnumerator SetupNewGameHeroineCoroutine(Guid newGameGuid, SaveData.Heroine heroine)
        {
            Logger.LogDebug($"{nameof(SetupNewGameHeroineCoroutine)}: start");
            GameDialogHelperCharaController controller;

            while ((controller = heroine.GetGameDialogHelperController()) == null)
            {
                yield return null;
            }

            Logger.LogDebug($"{nameof(SetupNewGameHeroineCoroutine)}: controller available");
            controller.SetExtendedData(null);
            GameDialogHelper.Instance.CurrentSaveGuid = newGameGuid;
            controller.PersistToCard();
            Logger.LogDebug($"{nameof(SetupNewGamePlayerCoroutine)}: done");
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            GameDialogHelper.DoReset();
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

            Logger.LogDebug($"{nameof(CheckHeroineMapping)}: start: {_heroineMap.Count}");
            var previousMap = new Dictionary<string, Guid>();
            foreach (var entry in _heroineMap)
            {
                previousMap[entry.Key] = new Guid(entry.Value.ToByteArray());
                Logger.LogDebug($"{nameof(CheckHeroineMapping)}: saved: {previousMap[entry.Key]}");
            }

            Logger.LogDebug($"{nameof(CheckHeroineMapping)}: saved: {previousMap.Count}");
            _heroineMap.Clear();
            for (var i = 0; i < Game.Instance.HeroineList.Count; i++)
            {
                var heroine = Game.Instance.HeroineList[i];
                var key = MappingKey(heroine);
                var currentGuid = _heroineMap[key] = heroine.GetHeroineGuid();
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
                    var mem = controller.LoadMemoryFromCard();
                    if (mem != null) memoryGuid = mem.HeroineGuid;
                }

                Logger.LogDebug(
                    $"{nameof(CheckHeroineMapping)}: {i:D5} {currentGuid} {previousGuid} {memoryGuid} {heroine.Name}");

                if (controller != null && !currentGuid.Equals(memoryGuid))
                {
                    if (memoryGuid.Equals(Guid.Empty) || memoryGuid.Equals(previousGuid))
                    {
                        controller.ProcessGuidChange(currentGuid);

                    }
                    else
                    {
                        Logger.LogError(
                            $"{nameof(CheckHeroineMapping)}: {i:D5} {heroine.Name}: dialog memory GUID mismatch");
                    }
                }


                if (!previousFound) continue;

                if (previousGuid.Equals(currentGuid) || previousGuid.Equals(Guid.Empty)) continue;

                if (!previousGuid.Equals(Guid.Empty))
                {
                    GameDialogHelperCharaController.HeroineGuidMap[previousGuid] = currentGuid;

                    if (controller != null)
                    {
                        controller.ProcessGuidChange(currentGuid);
                        continue;
                    }
                }

                Logger.LogError(
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
                Logger.LogDebug(
                    $"new save guid: {GameDialogHelper.Instance.CurrentSaveGuid}");
            }

            Game.Instance.Player.chaCtrl.GetGameDialogHelperController().SafeProc(c => c.PersistToCard());
        }

        public static void DoReset()
        {
            var controller =
                GameAPI.GetRegisteredBehaviour(
                    typeof(GameDialogHelperGameController)) as GameDialogHelperGameController;
            if (controller == null)
            {
                Logger.LogWarning(
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
