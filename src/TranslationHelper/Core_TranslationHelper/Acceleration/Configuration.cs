using System;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace TranslationHelperPlugin.Acceleration
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        private static ConfigEntry<bool> EnableAcceleration { get; set; }

        internal static bool AccelerationEnabled { get; private set; }

        internal static void Setup()
        {
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");


            EnableAcceleration = TranslationHelper.Instance.Config.Bind("Acceleration", "Enable", true,
                "Enable game specific optimizations");
            EnableAcceleration.SettingChanged += EnableAcceleration_SettingChanged;
            EnableAcceleration_SettingChanged(null, EventArgs.Empty);

            var harmony = Hooks.SetupHooks();

            GameSpecificSetup(harmony);
        }

        private static void EnableAcceleration_SettingChanged(object sender, EventArgs e)
        {
            AccelerationEnabled = EnableAcceleration.Value;
        }
    }
}
