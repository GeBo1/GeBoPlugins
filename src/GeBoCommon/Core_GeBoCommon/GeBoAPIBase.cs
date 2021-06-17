using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.AutoTranslation.Implementation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using KKAPI;
using KKAPI.Maker;
using UnityEngine;
using XUAPluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;


namespace GeBoCommon
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(XUAPluginData.Identifier, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.MainGameProcessName)]
#if KK || AI
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
#if KK || HS2
    [BepInProcess(Constants.MainGameProcessNameVR)]
#endif
#if KK
    [BepInProcess(Constants.MainGameProcessNameVRSteam)]
#endif
#if HS
    [BepInProcess(Constants.BattleArenaProcessName)]
#endif
#if KKS
    [BepInProcess(Constants.TrialProcessName)]
#endif
    public partial class GeBoAPI : BaseUnityPlugin, IGeBoAPI
    {
        public const string GUID = "com.gebo.BepInEx.GeBoAPI";
        public const string PluginName = "GeBo Modding API";
        public const string Version = "1.1.2.1";

        private static readonly Dictionary<string, bool> NotificationSoundsEnabled = new Dictionary<string, bool>();

        /// <summary>
        ///     Gets the instance of GeBoAPI for the current execution.
        /// </summary>
        /// <value>
        ///     The instance.
        /// </value>
        public static GeBoAPI Instance => _instance != null ? _instance : _instance = FindObjectOfType<GeBoAPI>();

        internal new ManualLogSource Logger => base.Logger;

        private static GeBoAPI _instance;

        private readonly SimpleLazy<IAutoTranslationHelper> _autoTranslationHelper =
            new SimpleLazy<IAutoTranslationHelper>(AutoTranslationHelperLoader);

        /// <inheritdoc />
        public IAutoTranslationHelper AutoTranslationHelper => _autoTranslationHelper.Value;

        public int ChaFileNameCount => ChaFileNamesInternal.Count;

        private readonly SimpleLazy<IList<string>> _chaFileNames =
            new SimpleLazy<IList<string>>(() => ChaFileNamesInternal.Select(n => n.Key).ToList());

        /// <inheritdoc />
        public IList<string> ChaFileNames => _chaFileNames.Value;

        private static ConfigEntry<bool> EnableObjectPoolsConfig { get; set; }


        public static bool EnableObjectPools { get; private set; }

#if TIMERS
        private static ConfigEntry<bool> TimersEnabledConfig { get; set; }
        public static bool TimersEnabled { get; private set; }
#endif
        public static event EventHandler<EventArgs> TranslationsLoaded;

        internal static void OnTranslationLoaded(EventArgs eventArgs)
        {
            TranslationsLoaded?.SafeInvoke(
                Instance == null ? null : Instance.AutoTranslationHelper, eventArgs);
        }

        private void Awake()
        {
            _instance = this;
            EnableObjectPoolsConfig = Config.Bind("Developer Settings", "Enable ObjectPools", true,
                new ConfigDescription("Leave enabled unless requested otherwise", null, "Advanced"));
            EnableObjectPoolsConfig.SettingChanged += EnableObjectPoolsConfig_SettingChanged;
            EnableObjectPools = EnableObjectPoolsConfig.Value;

            Common.SetCurrentLogger(Logger);

#if TIMERS
            TimersEnabledConfig = Config.Bind("Developer Settings", "Enable Timer Messages", false,
                new ConfigDescription("Adds log messages for how long certain events take.", null, "Advanced"));
            TimersEnabledConfig.SettingChanged += TimersEnabledConfig_SettingChanged;
            TimersEnabledConfig_SettingChanged(this, EventArgs.Empty);
#endif
        }

        private void MakerStartupTimerStart(object sender, RegisterCustomControlsEvent e)
        {
            var startTime = Time.realtimeSinceStartup;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
        }

        private void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void EnableObjectPoolsConfig_SettingChanged(object sender, EventArgs e)
        {
            EnableObjectPools = EnableObjectPoolsConfig.Value;
        }

#if Timers
        private void TimersEnabledConfig_SettingChanged(object sender, EventArgs e)
        {
            TimersEnabled = TimersEnabledConfig.Value;
            Timers.Setup();
        }
#endif

        private static IAutoTranslationHelper AutoTranslationHelperLoader()
        {
            if (Chainloader.PluginInfos.ContainsKey(XUAPluginData.Identifier))
            {
                return new XUnityAutoTranslationHelper();
            }

            return new StubAutoTranslationHelper();
        }

        /// <summary>
        ///     Setups the notification sound configuration for a plugin.
        /// </summary>
        /// <param name="guid">The plugin GUID you're configuring notifications for.</param>
        /// <param name="configEntry">The configuration entry.</param>
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

        /// <summary>
        ///     Plays the notification sound.
        /// </summary>
        /// <param name="notificationSound">The notification sound to play </param>
        /// <param name="guid">The plugin GUID. If provided will check the per-plugin config if sounds are enabled.</param>
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

        /// <inheritdoc />
        public int ChaFileNameToIndex(string chaName)
        {
            return ChaFileNames.IndexOf(chaName);
        }

        /// <inheritdoc />
        public NameType ChaFileIndexToNameType(int index)
        {
            if (index < 0 || index > ChaFileNamesInternal.Count) return NameType.Unclassified;
            return ChaFileNamesInternal[index].Value;
        }

        /// <inheritdoc />
        public string ChaFileIndexToName(int index)
        {
            if (index < 0 || index > ChaFileNamesInternal.Count) return null;
            return ChaFileNamesInternal[index].Key;
        }

        private void OnDestroy()
        {
            Common.SetCurrentLogger(null);
        }
    }
}
