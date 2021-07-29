using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using UnityEngine;

namespace StudioSceneInitialCameraPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioSceneInitialCamera : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.studioinitialcamera";
        public const string PluginName = "Studio Scene Initial Camera";
        public const string Version = "0.7.0.0";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> SaveInitialCamera { get; private set; }
        public static ConfigEntry<bool> CreateStudioCameraObject { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SelectInitialCameraShortcut { get; private set; }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
            Enabled = Config.Bind("Config", "Enabled", true, new ConfigDescription(
                "Whether the plugin is enabled", null, new ConfigurationManagerAttributes {Order = 100}));
            SelectInitialCameraShortcut = Config.Bind("Config", "Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.BackQuote),
                new ConfigDescription("Key than changes to this camera (behaves like 1-0)", null,
                    new ConfigurationManagerAttributes {Order = 80}));
            SaveInitialCamera = Config.Bind("Save Camera", "Save Initial Camera", true,
                new ConfigDescription(
                    "Will attempt to save the initial camera to an unused scene camera button after scene is loaded",
                    null, new ConfigurationManagerAttributes {Order = 2}));

            CreateStudioCameraObject = Config.Bind("Save Camera", "Create Studio Camera Object", false,
                new ConfigDescription(
                    "If 'Save Initial Camera' is enabled, but the plugin is unable to find an unused camera slot, " +
                    "a special studio camera object will be a added to the scene. When activated it will jump to " +
                    "the initial camera instead.", null,
                    new ConfigurationManagerAttributes {IsAdvanced = true, Order = 1}));
            StudioSaveLoadApi.RegisterExtraBehaviour<StudioSceneInitialCameraController>(GUID);
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        public static StudioSceneInitialCameraController GetController()
        {
            var container = Traverse.Create(typeof(StudioSaveLoadApi))
                ?.Field<GameObject>("_functionControllerContainer")?.Value;
            return container == null ? null : container.GetComponent<StudioSceneInitialCameraController>();
        }
    }
}
