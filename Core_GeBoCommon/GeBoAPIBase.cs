using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using GeBoCommon.Utilities;
#if AI
using AIChara;
#endif

namespace GeBoCommon
{
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.GameProcessName)]
#if KK
    [BepInProcess(Constants.AltGameProcessName)]
#endif
#if HS
    [BepInProcess(Constants.BattleArenaProcessName)]
#endif
    public partial class GeBoAPI : BaseUnityPlugin, IGeBoAPI
    {
        public const string GUID = "com.gebo.BepInEx.GeBoAPI";
        public const string PluginName = "GeBo Modding API";
        public const string Version = "0.9.0";

        private static readonly Dictionary<string, bool> NotificationSoundsEnabled = new Dictionary<string, bool>();
        public static GeBoAPI Instance { get; private set; }

        internal new ManualLogSource Logger;

        public GeBoAPI()
        {
            autoTranslationHelper = new SimpleLazy<AutoTranslation.IAutoTranslationHelper>(AutoTranslationHelperLoader);
        }

        internal void Main()
        {
            Instance = this;
            Logger = base.Logger;
        }

        private readonly SimpleLazy<AutoTranslation.IAutoTranslationHelper> autoTranslationHelper;
        public AutoTranslation.IAutoTranslationHelper AutoTranslationHelper => autoTranslationHelper.Value;

        private AutoTranslation.IAutoTranslationHelper AutoTranslationHelperLoader()
        {
            if (Chainloader.PluginInfos.ContainsKey(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier))
            {
                return new AutoTranslation.Implementation.XUnityAutoTranslationHelper();
            }
            return new AutoTranslation.Implementation.StubAutoTranslationHelper();
        }

        public void SetupNotificationSoundConfig(string guid, ConfigEntry<bool> configEntry)
        {
            NotificationSoundsEnabled[guid] = configEntry.Value;
            configEntry.SettingChanged += new EventHandler((sender, e) => NotificationSound_SettingChanged(guid, sender, e));
        }

        private void NotificationSound_SettingChanged(string guid, object sender, EventArgs e)
        {
            if (e is null)
            {
                return;
            }
            var entry = sender as ConfigEntry<bool>;
            NotificationSoundsEnabled[guid] = entry.Value;
        }

        public void PlayNotificationSound(NotificationSound notificationSound, string guid = null)
        {
            if (!guid.IsNullOrEmpty())
            {
                if (NotificationSoundsEnabled.TryGetValue(guid, out bool enabled) && !enabled)
                {
                    return;
                }
            }
            PlayNotification(notificationSound);
        }
    }
}
