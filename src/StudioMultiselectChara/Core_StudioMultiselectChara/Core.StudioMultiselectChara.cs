using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Studio;
using GeBoCommon.Utilities;
using KKAPI.Studio;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace StudioMultiSelectCharaPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioMultiSelectChara : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.studiomultiselectchara";
        public const string PluginName = "Studio MultiSelect Chara";
        public const string Version = "1.0.0.1";
        internal static new ManualLogSource Logger;

        private bool _busy;

        internal void Update()
        {
            if (_busy || !Enabled.Value || !MultiSelectShortcut.Value.IsDown()) return;
            _busy = true;
            StartCoroutine(UpdateSelectionsCoroutine().AppendCo(() => _busy = false));
        }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
            Enabled = Config.Bind("Config", "Enabled", true, "Whether the plugin is enabled");
            MultiSelectShortcut = Config.Bind("Keyboard Shortcuts", "Perform multi-select",
                new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift),
                "Select all instances of the currently selected character");
            NotificationSoundsEnabled = Config.Bind("Config", "Notification Sounds", true,
                "When enabled, notification sounds will play when selection is complete");
            GeBoAPI.Instance.SetupNotificationSoundConfig(GUID, NotificationSoundsEnabled);
        }

        private static bool DoesCharaMatch(CharaId charaId, OCIChar test)
        {
            return charaId == test.GetMatchId();
        }

        public static IEnumerable<CharaId> GetSelectedMatchIds()
        {
            return StudioAPI.GetSelectedCharacters().Select(c => c.GetMatchId()).Distinct();
        }

        private static void SelectCharasById(CharaId matchId)
        {
            var selected = 0;
            var origCharCount = StudioAPI.GetSelectedCharacters().Count();
            var origObjCount = StudioAPI.GetSelectedObjects().Count() - origCharCount;
            foreach (var objectCtrlInfo in StudioUtils.EnumerateObjects())
            {
                Logger.DebugLogDebug($"SelectCharasById: {objectCtrlInfo}");
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

            Logger.LogInfoMessage(
                $"characters selected: {selected} ({selected - origCharCount} new selections, {origObjCount} non-characters unselected)");
            GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Success);
        }

        private IEnumerator UpdateSelectionsCoroutine()
        {
            var selectedIds = GetSelectedMatchIds().ToList();
            var selectedCount = selectedIds.Count;
            if (selectedCount == 0)
            {
                Logger.LogWarningMessage("No characters selected");
                GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Error);
            }
            else if (selectedCount != 1)
            {
                Logger.LogWarningMessage(
                    "Select only instances of a single character.");
                GeBoAPI.Instance.PlayNotificationSound(NotificationSound.Error);
            }
            else
            {
                yield return
                    StartCoroutine(CoroutineUtils.CreateCoroutine(() => SelectCharasById(selectedIds.First())));
            }
        }

        #region configuration

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<KeyboardShortcut> MultiSelectShortcut { get; private set; }
        public static ConfigEntry<bool> NotificationSoundsEnabled { get; private set; }

        #endregion configuration
    }
}
