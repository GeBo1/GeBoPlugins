using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using UnityEngine;

namespace StudioSceneInitialCameraPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    public partial class StudioSceneInitialCamera : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.studioinitialcamera";
        public const string PluginName = "Studio Initial Camera";
        public const string Version = "0.0.5";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SelectInitialCameraShortcut { get; private set; }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
            Enabled = Config.Bind("Config", "Enabled", true, "Whether the plugin is enabled");
            SelectInitialCameraShortcut = Config.Bind("Config", "Keyboard Shortcut",
                new KeyboardShortcut(KeyCode.BackQuote),
                "Key than changes to this camera (behaves like 1-0)");
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
