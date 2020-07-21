using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.AutoTranslation.Implementation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using XUnity.AutoTranslator.Plugin.Core.Constants;


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
        public const string Version = "1.0";

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

        public int ChaFileNameCount => ChaFileNamesInternal.Count;

        private readonly SimpleLazy<IList<string>> _chaFileNames =
            new SimpleLazy<IList<string>>(() => ChaFileNamesInternal.Select(n => n.Key).ToList());

        public IList<string> ChaFileNames => _chaFileNames.Value;

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

            void SoundSettingChanged(object sender, EventArgs e)
            {
                if (e is null) return;
                if (sender is ConfigEntry<bool> entry) NotificationSoundsEnabled[guid] = entry.Value;
            }

            configEntry.SettingChanged += SoundSettingChanged;
        }

        public void PlayNotificationSound(NotificationSound notificationSound, string guid = null)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                if (NotificationSoundsEnabled.TryGetValue(guid, out var soundEnabled) && !soundEnabled)
                {
                    return;
                }
            }

            PlayNotification(notificationSound);
        }

        public int ChaFileNameToIndex(string chaName)
        {
            return ChaFileNames.IndexOf(chaName);
        }

        public NameType ChaFileIndexToNameType(int index)
        {
            if (index < 0 || index > ChaFileNamesInternal.Count) return NameType.Unclassified;
            return ChaFileNamesInternal[index].Value;
        }

        public string ChaFileIndexToName(int index)
        {
            if (index < 0 || index > ChaFileNamesInternal.Count) return null;
            return ChaFileNamesInternal[index].Key;
        }
    }
}
