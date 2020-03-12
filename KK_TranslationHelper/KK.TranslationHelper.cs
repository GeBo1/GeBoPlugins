using BepInEx;
using KKAPI;
using UnityEngine.SceneManagement;
using Manager;
using System;
using GeBoCommon;

namespace TranslationHelperPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        private WeakReference RegisteredPlayer = null;

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
            SaveData.Player player = Singleton<Game>.Instance?.Player;
            if (RegisteredPlayer != null && RegisteredPlayer.Target != player)
            {
                if (RegisteredPlayer.IsAlive)
                {
                    SaveData.Player oldPlayer = RegisteredPlayer.Target as SaveData.Player;
                    StartCoroutine(UnregisterReplacements(oldPlayer.charFile));
                }
                RegisteredPlayer = null;
            }
            if (player != null && RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode()))
            {
                StartCoroutine(RegisterReplacementsWrapper(player.charFile));
                RegisteredPlayer = new WeakReference(player);
            }
        }
    }
}