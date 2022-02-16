using System.Collections;
using System.Linq;
using AIChara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameWhoIsTherePlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class GameWhoIsThere : BaseUnityPlugin
    {
        [PublicAPI]
        public const string GUID = Constants.PluginGUIDPrefix + "." + nameof(GameWhoIsThere);

        public const string PluginName = "Who Is There?";
        public const string Version = "1.0.1.3";

        private static GameWhoIsThere _instance;

        internal static new ManualLogSource Logger;
        private readonly ChaFileControl[] _chaFileControls = { null, null };

        private readonly Text[] _labels = { null, null };
        private bool _busy;
        private bool _keyIdle = true;

        public static GameWhoIsThere Instance => PluginUtils.InstanceGetter(ref _instance);

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ShowWhoIsThereShortcut { get; private set; }

        internal bool Active => InMyRoom && _labels.Any(x => x != null);
        internal bool InMyRoom { get; private set; }

        internal void Reset()
        {
            for (var i = 0; i < _labels.Length; i++)
            {
                _labels[i] = null;
                _chaFileControls[i] = null;
            }
        }

        internal void Update()
        {
            if (!Active || _busy) return;

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

        internal void Main()
        {
            _instance = this;
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


        private IEnumerator UpdateLabels()
        {
            if (_busy) yield break;
            _busy = true;
            yield return null;
            try
            {
                for (var i = 0; i < _labels.Length; i++)
                {
                    var cfc = _chaFileControls.SafeGet(i);
                    if (cfc == null) continue;
                    _labels.SafeProc(i, l => l.text = l.text == "???" ? $"({cfc.parameter.fullname})" : "???");
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
    }
}
