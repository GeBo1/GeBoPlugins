using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;

namespace StudioCharaAnimFixPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioCharaAnimFix : BaseUnityPlugin
    {
        [PublicAPI]
        public const string GUID = Constants.PluginGUIDPrefix + "." + nameof(StudioCharaAnimFix);

        public const string PluginName = "Studio Chara Anim Fix";
        public const string Version = "0.5.0.0";

        internal static new ManualLogSource Logger;
        internal static bool FixAnimSlidersOnLoadEnabled;
        internal static bool FixNeckLookOnReplaceEnabled;
        internal static bool FixEyeLookOnLoadEnabled;


        private static readonly HashSet<OCIChar> PendingChars = new HashSet<OCIChar>();

        private static readonly Dictionary<OCIChar, List<byte[]>> SavedNeckLookData =
            new Dictionary<OCIChar, List<byte[]>>();

        internal static bool FixesEnabled =>
            FixNeckLookOnReplaceEnabled || FixAnimSlidersOnLoadEnabled || FixEyeLookOnLoadEnabled;

        private static ConfigEntry<bool> Enabled { get; set; }
        private static ConfigEntry<bool> FixAnimSlidersOnLoad { get; set; }
        private static ConfigEntry<bool> FixNeckLookOnReplace { get; set; }
        private static ConfigEntry<bool> FixEyeLookOnLoad { get; set; }

        private void Awake()
        {
            Logger = Logger ?? base.Logger;

            Enabled = InitConfigEntry("Config", "Enabled", true, new ConfigDescription(
                "Whether the plugin is enabled", null, new ConfigurationManagerAttributes { Order = 100 }));

            FixAnimSlidersOnLoad = InitConfigEntry("Config", "Fix animation sliders on load", true,
                "Ensure the extra animation sliders are applied on scene load, and after characters are replaced");

            FixEyeLookOnLoad = InitConfigEntry("Config", "Fix eye look on load", true,
                "Ensure the eye look settings are applied on scene load, and after characters are replaced");

            FixNeckLookOnReplace = InitConfigEntry("Config", "Fix neck look on replace", true,
                "Fixes issue where 'fixed' neck look information can be lost when a character is replaced in a scene");
        }

        private void Start()
        {
            Logger = Logger ?? base.Logger;
            UpdateConfiguration(this, EventArgs.Empty);
            CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;
            StudioSaveLoadApi.SceneLoad += StudioSaveLoadApi_SceneLoad;
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private void UpdateConfiguration(object sender, EventArgs e)
        {
            FixAnimSlidersOnLoadEnabled = Enabled.Value && FixAnimSlidersOnLoad.Value;
            FixNeckLookOnReplaceEnabled = Enabled.Value && FixNeckLookOnReplace.Value;
            FixEyeLookOnLoadEnabled = Enabled.Value && FixEyeLookOnLoad.Value;

            if (!FixesEnabled) PendingChars.Clear();
            if (!FixNeckLookOnReplaceEnabled) SavedNeckLookData.Clear();
        }


        private ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue,
            string description)
        {
            return InitConfigEntry(section, key, defaultValue, new ConfigDescription(description));
        }

        private ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue,
            ConfigDescription configDescription)
        {
            var result = Config.Bind(section, key, defaultValue, configDescription);
            result.SettingChanged += UpdateConfiguration;
            return result;
        }

        private static void SaveNeckLookData(OCIChar studioChar)
        {
            if (!SavedNeckLookData.TryGetValue(studioChar, out var neckLookDataList))
            {
                SavedNeckLookData[studioChar] = neckLookDataList = new List<byte[]>();
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    studioChar.neckLookCtrl.SaveNeckLookCtrl(writer);
                }

                stream.Flush();
                neckLookDataList.Add(stream.GetBuffer());
            }
        }

        internal bool TryApplySavedNeckLookData(OCIChar studioChar)
        {
            if (!SavedNeckLookData.TryGetValue(studioChar, out var neckLookDataList)) return false;
            neckLookDataList.Reverse();
            var loaded = false;
            Logger.DebugLogDebug($"applying neck look data for {studioChar.charInfo}");
            try
            {
                foreach (var bytes in neckLookDataList)
                {
                    using (var stream = new MemoryStream(bytes))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            try
                            {
                                studioChar.neckLookCtrl.LoadNeckLookCtrl(reader);
                                loaded = true;
                            }
                            catch (Exception err)
                            {
                                Logger.LogException(err,
                                    $"{nameof(TryApplySavedNeckLookData)}: unable to read saved data");
                            }
                        }
                    }
                }
            }
            finally
            {
                SavedNeckLookData.Remove(studioChar);
            }

            return loaded;
        }

        private void StudioSaveLoadApi_SceneLoad(object sender, SceneLoadEventArgs e)
        {
            if (e.Operation != SceneOperationKind.Import)
            {
                PendingChars.Clear();
                SavedNeckLookData.Clear();
            }

            if (e.Operation == SceneOperationKind.Clear || !FixesEnabled) return;

            foreach (var obj in e.LoadedObjects.Values)
            {
                if (obj is OCIChar studioChar) FixCharaAnim(studioChar);
            }
        }

        private void CharacterApi_CharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            if (!FixesEnabled || StudioSaveLoadApi.LoadInProgress) return;
            e.ReloadedCharacter.SafeProc(c => c.GetOCIChar().SafeProc(FixCharaAnim));
        }

        private void FixCharaAnim(OCIChar studioChar)
        {
            if (PendingChars.Contains(studioChar)) return;
            PendingChars.Add(studioChar);
            Logger.DebugLogDebug($"queueing fix for {studioChar.charInfo}");
            StartCoroutine(FixCharaAnimCoroutine(studioChar));
        }

        private static void ApplyAnimationSliders(OCIChar studioChar)
        {
            Logger.DebugLogDebug($"applying animation sliders for {studioChar.charInfo}");
            studioChar.animeOptionParam1 = studioChar.animeOptionParam1;
            studioChar.animeOptionParam2 = studioChar.animeOptionParam2;
        }

        private static void ApplyEyeLook(OCIChar studioChar)
        {
            Logger.DebugLogDebug($"applying eye look for {studioChar.charInfo}");
            studioChar.ChangeLookEyesPtn(studioChar.charInfo.fileStatus.eyesLookPtn);
        }

        private IEnumerator FixCharaAnimCoroutine(OCIChar studioChar)
        {
            yield return null;
            if (!PendingChars.Contains(studioChar)) yield break;
            if (FixNeckLookOnReplaceEnabled) TryApplySavedNeckLookData(studioChar);
            yield return CoroutineUtils.WaitForEndOfFrame;
            PendingChars.Remove(studioChar);
            if (FixAnimSlidersOnLoadEnabled) ApplyAnimationSliders(studioChar);
            if (FixEyeLookOnLoadEnabled) ApplyEyeLook(studioChar);
        }
    }
}
