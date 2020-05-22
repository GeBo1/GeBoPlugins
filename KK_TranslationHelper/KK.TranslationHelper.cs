using BepInEx;
using KKAPI;
using UnityEngine.SceneManagement;
using Manager;
using System;

namespace TranslationHelperPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        private WeakReference RegisteredPlayer;

        internal void GameSpecificAwake()
        {
            SplitNamesBeforeTranslate = false;
        }

        internal void GameSpecificStart()
        {
            SplitNamesBeforeTranslate = false;
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame)
            {
                SceneManager.activeSceneChanged += RegisterPlayer;
            }
        }

        private void RegisterPlayer(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            var player = Singleton<Game>.Instance?.Player;
            if (RegisteredPlayer != null && RegisteredPlayer.Target != player)
            {
                if (RegisteredPlayer.IsAlive)
                {
                    var oldPlayer = RegisteredPlayer.Target as SaveData.Player;
                    StartCoroutine(UnregisterReplacements(oldPlayer?.charFile));
                }
                RegisteredPlayer = null;
            }

            if (player == null || !RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode())) return;

            StartCoroutine(RegisterReplacementsWrapper(player.charFile));
            RegisteredPlayer = new WeakReference(player);
        }
    }
}
