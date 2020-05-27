using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using KKAPI.Studio;
using KKAPI.Utilities;
using Studio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepinLogLevel = BepInEx.Logging.LogLevel;
using KeyboardShortcut = BepInEx.Configuration.KeyboardShortcut;
#if AI
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioMultiSelectChara
    {
        internal static new ManualLogSource Logger;
        public const string GUID = "com.gebo.BepInEx.studiomultiselectchara";
        public const string PluginName = "Studio Multiselect Chara";
        public const string Version = "0.9.0";

        private bool _busy;

        #region configuration

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<KeyboardShortcut> MultiselectShortcut { get; private set; }
        public static ConfigEntry<bool> NotificationSoundsEnabled { get; private set; }

        #endregion configuration

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
            Enabled = Config.Bind("Config", "Enabled", true, "Whether the plugin is enabled");
            MultiselectShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Next", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift), "Perform multiselect");
            NotificationSoundsEnabled = Config.Bind("Config", "Notification Sounds", true, "When enabled, notification sounds will play when selection is complete");
            GeBoAPI.Instance.SetupNotificationSoundConfig(GUID, NotificationSoundsEnabled);
        }

        internal void Update()
        {
            if (Enabled.Value && MultiselectShortcut.Value.IsDown() && !_busy)
            {
                _busy = true;
                StartCoroutine(UpdateSelectionsCoroutine().AppendCo(() => _busy = false));
            }
        }

        private static bool DoesCharaMatch(CharaId charaId, OCIChar test)
        {
            return charaId == test.GetMatchId();
        }

        public static IEnumerable<CharaId> GetSelectedMatchIds()
        {
            return StudioAPI.GetSelectedCharacters().Select((c) => c.GetMatchId()).Distinct();
        }

        private IEnumerable<TreeNodeObject> EnumerateTreeNodeObjects(TreeNodeObject root = null)
        {
            var roots = new List<TreeNodeObject>();
            if (root != null)
            {
                roots.Add(root);
            }
            else
            {
                root = StudioAPI.GetSelectedObjects().FirstOrDefault()?.treeNodeObject;
                if (root != null)
                {
                    roots.AddRange(root.GetTreeNodeCtrl().GetTreeNodeObjects());
                }
            }
            foreach (var entry in roots)
            {
                yield return entry;
                foreach (var childNode in entry.child)
                {
                    foreach (var tnObj in EnumerateTreeNodeObjects(childNode))
                    {
                        yield return tnObj;
                    }
                }
            }
        }

        private IEnumerable<ObjectCtrlInfo> EnumerateObjects(ObjectCtrlInfo root = null)
        {
            TreeNodeObject tnRoot = null;
            if (root != null)
            {
                tnRoot = root.treeNodeObject;
            }
            foreach (var tnObj in EnumerateTreeNodeObjects(tnRoot))
            {
                if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(tnObj, out var result))
                {
                    yield return result;
                }
            }
        }

        private void SelectCharasById(CharaId matchId)
        {
            var selected = 0;
            var origCharCount = StudioAPI.GetSelectedCharacters().Count();
            var origObjCount = StudioAPI.GetSelectedObjects().Count() - origCharCount;
            foreach (var objectCtrlInfo in EnumerateObjects())
            {
                Logger.Log(BepinLogLevel.Debug, $"SelectCharasById: {objectCtrlInfo}");
                if (objectCtrlInfo is OCIChar ociChar)
                {
                    if (DoesCharaMatch(matchId, ociChar))
                    {
                        ociChar.MultiSelectInWorkarea();
                        selected++;
                    }
                    else
                    {
                        if (ociChar.IsSelectedInWorkarea())
                        {
                            ociChar.UnselectInWorkarea();
                        }
                    }
                }
                else
                {
                    objectCtrlInfo.UnselectInWorkarea();
                }
            }
            Logger.Log(BepinLogLevel.Info | BepinLogLevel.Message, $"characters selected: {selected} ({selected - origCharCount} new selections, {origObjCount} non-characters unselected)");
            GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Success);
        }

        private IEnumerator UpdateSelectionsCoroutine()
        {
            var selectedIds = GetSelectedMatchIds().ToList();
            var selectedCount = selectedIds.Count;
            if (selectedCount == 0)
            {
                Logger.Log(BepinLogLevel.Warning | BepinLogLevel.Message, "No characters selected");
                GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Error);
            }
            else if (selectedCount != 1)
            {
                Logger.Log(BepinLogLevel.Warning | BepinLogLevel.Message, "Select only instances of a single character.");
                GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Error);
            }
            else
            {
                yield return StartCoroutine(CoroutineUtils.CreateCoroutine(() => SelectCharasById(selectedIds.First())));
            }
        }
    }
}
