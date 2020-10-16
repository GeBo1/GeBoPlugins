using System.Collections;
using System.Linq;
using AIChara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameWhoIsTherePlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class GameWhoIsThere : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.bepinex.whoisthere";
        public const string PluginName = "Who Is There?";
        public const string Version = "1.0";

        public static GameWhoIsThere Instance;
        internal static new ManualLogSource Logger;
        private readonly ChaFileControl[] _chaFileControls = {null, null};

        private readonly Text[] _labels = {null, null};
        private bool _busy;
        private bool _keyIdle = true;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ShowWhoIsThereShortcut { get; private set; }

        internal bool Active => InMyRoom && _labels.Any(x => x != null);
        internal bool InMyRoom { get; private set; }

        internal void Main()
        {
            Instance = this;
            Logger = Logger ?? base.Logger;

            Enabled = Config.Bind("Settings", "Enabled", true, "Whether the plugin is enabled");
            ShowWhoIsThereShortcut = Config.Bind("Keyboard Shortcuts", "Toggle showing who is in room",
                new KeyboardShortcut(KeyCode.Slash),
                "Display name of character in surprise events");

            Reset();
            Hooks.SetupHooks();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }


        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name == "MyRoom" && Enabled.Value) InMyRoom = true;
        }


        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (!InMyRoom) return;
            var wasActive = Active;
            InMyRoom = false;
            if (!Active && wasActive) Reset();
        }

        internal void Update()
        {
            if (!Active || _busy || _labels[0] == null) return;

            var isPressed = ShowWhoIsThereShortcut.Value.IsPressed();
            if (_keyIdle)
            {
                if (!isPressed) return;
                _keyIdle = false;
                StartCoroutine(UpdateLabels());
            }
            else
            {
                if (!isPressed) _keyIdle = true;
            }
        }


        private IEnumerator UpdateLabels()
        {
            if (_busy) yield break;
            _busy = true;
            yield return null;
            try
            {
                for (var i = 0; i < _labels.Length; i++)
                {
                    if (_labels[i] == null || _chaFileControls[i] == null) continue;
                    _labels[i].text = _labels[i].text == "???" ? $"({_chaFileControls[i].parameter.fullname})" : "???";
                }

                yield return null;
            }
            finally
            {
                _busy = false;
            }
        }

        internal void ConfigureDisplay(int index, Text label, ChaFileControl who)
        {
            _labels[index] = label;
            _chaFileControls[index] = who;
        }

        internal void Reset()
        {
            for (var i = 0; i < _labels.Length; i++)
            {
                _labels[i] = null;
                _chaFileControls[i] = null;
            }
        }
    }
}
