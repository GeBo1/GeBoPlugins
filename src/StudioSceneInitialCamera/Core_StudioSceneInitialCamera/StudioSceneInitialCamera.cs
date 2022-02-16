using System;
using System.Linq.Expressions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using UnityEngine;

namespace StudioSceneInitialCameraPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(AutosavePluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioSceneInitialCamera : BaseUnityPlugin
    {
        [PublicAPI]
        public const string GUID = Constants.PluginGUIDPrefix + "." + nameof(StudioSceneInitialCamera);

        public const string PluginName = "Studio Scene Initial Camera";
        public const string Version = "0.7.0.1";

        private const string AutosavePluginGUID = "com.deathweasel.bepinex.autosave";
        internal static new ManualLogSource Logger;

        private static readonly SimpleLazy<Func<bool>> IsAutosavingHandler =
            new SimpleLazy<Func<bool>>(InitAutosavingHandler);

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> SaveInitialCameraConfig { get; private set; }
        public static ConfigEntry<bool> CreateStudioCameraObjectConfig { get; private set; }
        public static ConfigEntry<bool> PreserveCameraDuringAutosaveConfig { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SelectInitialCameraShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SelectPreviousInitialCameraShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SelectNextInitialCameraShortcut { get; private set; }

        public static bool SaveInitialCamera { get; private set; }
        public static bool CreateStudioCameraObject { get; private set; }
        public static bool PreserveCameraDuringAutosave { get; private set; }


        public static bool IsAutosaving
        {
            get
            {
                try
                {
                    return IsAutosavingHandler.Value();
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(IsAutosaving));
                    return false;
                }
            }
        }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
            Enabled = Config.Bind("Config", "Enabled", true, new ConfigDescription(
                "Whether the plugin is enabled", null, new ConfigurationManagerAttributes { Order = 100 }));

            SelectInitialCameraShortcut = Config.Bind("Config", "Activate Initial Camera",
                new KeyboardShortcut(KeyCode.BackQuote),
                new ConfigDescription("Key that changes to the initial camera (behaves like 1-9,0)", null,
                    new ConfigurationManagerAttributes { Order = 80 }));

            SelectPreviousInitialCameraShortcut = Config.Bind("Config", "Restore Previous Initial Camera",
                new KeyboardShortcut(KeyCode.BackQuote, KeyCode.LeftControl),
                new ConfigDescription("Attempt to restore previous saved camera state.", null,
                    new ConfigurationManagerAttributes { Order = 71 }));

            SelectNextInitialCameraShortcut = Config.Bind("Config", "Restore Next Initial Camera",
                new KeyboardShortcut(KeyCode.BackQuote, KeyCode.LeftAlt),
                new ConfigDescription("Attempt to restore more recent saved camera state.", null,
                    new ConfigurationManagerAttributes { Order = 70 }));

            PreserveCameraDuringAutosaveConfig = InitConfig("Save Camera", "Preserve Camera During Autosave", true,
                new ConfigDescription(
                    "Will attempt to preserve the initial camera when Autosave plugin saves scene.",
                    null, new ConfigurationManagerAttributes { Order = 4 }));

            SaveInitialCameraConfig = InitConfig("Save Camera", "Save Initial Camera", true,
                new ConfigDescription(
                    "Will attempt to save the initial camera to an unused scene camera button after scene is loaded",
                    null, new ConfigurationManagerAttributes { Order = 10 }));

            CreateStudioCameraObjectConfig = InitConfig("Save Camera", "Create Studio Camera Object", false,
                new ConfigDescription(
                    "If 'Save Initial Camera' is enabled, but the plugin is unable to find an unused camera slot, " +
                    "a special studio camera object will be a added to the scene. When activated it will jump to " +
                    "the initial camera instead.", null,
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

            UpdateConfiguration(this, EventArgs.Empty);
            StudioSaveLoadApi.RegisterExtraBehaviour<StudioSceneInitialCameraController>(GUID);
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private ConfigEntry<T> InitConfig<T>(
            string section,
            string key,
            T defaultValue,
            ConfigDescription configDescription = null)
        {
            var config = Config.Bind(section, key, defaultValue, configDescription);
            config.SettingChanged += UpdateConfiguration;
            return config;
        }

        private void UpdateConfiguration(object sender, EventArgs args)
        {
            SaveInitialCamera = Enabled.Value && SaveInitialCameraConfig.Value;
            CreateStudioCameraObject = Enabled.Value && CreateStudioCameraObjectConfig.Value;
            PreserveCameraDuringAutosave = SaveInitialCamera && PreserveCameraDuringAutosaveConfig.Value;
        }

        [PublicAPI]
        public static StudioSceneInitialCameraController GetController()
        {
            var container = Traverse.Create(typeof(StudioSaveLoadApi))
                ?.Field<GameObject>("_functionControllerContainer")?.Value;
            return container == null ? null : container.GetComponent<StudioSceneInitialCameraController>();
        }

        private static Func<bool> InitAutosavingHandler()
        {
            Expression<Func<bool>> getter = null;
            // public static bool Autosaving 
            if (Chainloader.PluginInfos.TryGetValue(AutosavePluginGUID, out var autosaveInfo))
            {
                var assembly = autosaveInfo.Instance.GetType().Assembly;
                var autosaveType = assembly.GetType("KK_Plugins.Autosave");
                if (autosaveType != null)
                {
                    var innerGetter = Delegates.FieldOrPropertyGetter<bool>(autosaveType, "Autosaving");
                    if (innerGetter != null) getter = () => innerGetter(null);
                }
            }

            if (getter == null) getter = () => false;
            return getter.Compile();
        }
    }
}
