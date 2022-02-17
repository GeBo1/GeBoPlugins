#if KK||KKS||AI
#define KKAPI_HAS_MAINGAME_CONTROLLER
#endif

using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using KKAPI.Studio;
#if KKAPI_HAS_MAINGAME_CONTROLLER
using System;
using System.Collections;
using GeBoCommon.Utilities;
using KKAPI.MainGame;
using UnityEngine;
#endif

namespace TranslationHelperPlugin.MainGame
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal const string GUID = TranslationHelper.GUID + ".maingame";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (StudioAPI.InsideStudio) return;
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();
#if KKAPI_HAS_MAINGAME_CONTROLLER
            GameAPI.RegisterExtraBehaviour<Controller>(GUID);
#endif
            GameSpecificSetup(harmony);
        }

#if KKAPI_HAS_MAINGAME_CONTROLLER
        private static Controller _instance;

        internal static Controller Instance => _instance != null
            ? _instance
            : _instance = GameAPI.GetRegisteredBehaviour(typeof(Controller), GUID) as Controller;

        private static MonoBehaviour CoroutineLauncher =>
            Instance != null ? (MonoBehaviour)Instance : TranslationHelper.Instance;

        internal static Coroutine StartCoroutine(IEnumerator routine)
        {
            return CoroutineLauncher.StartCoroutine(routine);
        }

        internal static Coroutine StartCoroutine(string methodName)
        {
            return CoroutineLauncher.StartCoroutine(methodName);
        }

        internal static Coroutine StartCoroutine(string methodName, object value)
        {
            return CoroutineLauncher.StartCoroutine(methodName, value);
        }


        public static event EventHandler<EventArgs> NewGame;
        public static event EventHandler<GameSaveLoadEventArgs> GameSave;
        public static event EventHandler<GameSaveLoadEventArgs> GameLoad;


        internal static void OnNewGame(object sender, EventArgs eventArgs)
        {
            NewGame?.SafeInvoke(sender, eventArgs);
        }

        internal static void OnGameSave(object sender, GameSaveLoadEventArgs eventArgs)
        {
            GameSave?.SafeInvoke(sender, eventArgs);
        }

        internal static void OnGameLoad(object sender, GameSaveLoadEventArgs eventArgs)
        {
            GameLoad?.SafeInvoke(sender, eventArgs);
        }
#endif
    }
}
