using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.AutoTranslation.Implementation;
using GeBoCommon.Utilities;
using XUnity.AutoTranslator.Plugin.Core.Constants;
#if AI
using AIChara;
#endif

namespace GeBoCommon
{
    [BepInDependency(PluginData.Identifier, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.MainGameProcessName)]
#if KK || AI
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
#if HS
    [BepInProcess(Constants.BattleArenaProcessName)]
#endif
    public partial class GeBoAPI : BaseUnityPlugin, IGeBoAPI
    {
        public const string GUID = "com.gebo.BepInEx.GeBoAPI";
        public const string PluginName = "GeBo Modding API";
        public const string Version = "0.9.1";

        private static readonly Dictionary<string, bool> NotificationSoundsEnabled = new Dictionary<string, bool>();
        public static GeBoAPI Instance { get; private set; }

        internal new ManualLogSource Logger;

        public GeBoAPI()
        {
            _autoTranslationHelper = new SimpleLazy<IAutoTranslationHelper>(AutoTranslationHelperLoader);
        }

        internal void Main()
        {
            Instance = this;
            Logger = base.Logger;
        }

        private readonly SimpleLazy<IAutoTranslationHelper> _autoTranslationHelper;
        public IAutoTranslationHelper AutoTranslationHelper => _autoTranslationHelper.Value;

        private IAutoTranslationHelper AutoTranslationHelperLoader()
        {
            if (Chainloader.PluginInfos.ContainsKey(PluginData.Identifier))
            {
                return new XUnityAutoTranslationHelper();
            }

            return new StubAutoTranslationHelper();
        }

        public void SetupNotificationSoundConfig(string guid, ConfigEntry<bool> configEntry)
        {
            NotificationSoundsEnabled[guid] = configEntry.Value;
            configEntry.SettingChanged += (sender, e) => NotificationSound_SettingChanged(guid, sender, e);
        }

        private void NotificationSound_SettingChanged(string guid, object sender, EventArgs e)
        {
            if (e is null)
            {
                return;
            }

            var entry = (ConfigEntry<bool>) sender;
            NotificationSoundsEnabled[guid] = entry.Value;
        }

        public void PlayNotificationSound(NotificationSound notificationSound, string guid = null)
        {
            if (!guid.IsNullOrEmpty())
            {
                if (NotificationSoundsEnabled.TryGetValue(guid, out var soundEnabled) && !soundEnabled)
                {
                    return;
                }
            }

            PlayNotification(notificationSound);
        }
    }
}
