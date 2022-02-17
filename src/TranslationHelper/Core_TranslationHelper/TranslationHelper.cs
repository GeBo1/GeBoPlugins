using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Studio;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Presets.Data;
using TranslationHelperPlugin.Translation;
using TranslationHelperPlugin.Utils;
using UnityEngine.SceneManagement;
using Configuration = TranslationHelperPlugin.Translation.Configuration;
using XUAPluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;
#if AI || HS2
using AIChara;

#endif

namespace TranslationHelperPlugin
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
#if KK||HS2||KKS
    [BepInProcess(Constants.MainGameProcessNameVR)]
#endif
#if KK
    [BepInProcess(Constants.MainGameProcessNameVRSteam)]
#endif
#if KK||AI
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
#if KKS
    [BepInProcess(Constants.TrialProcessName)]
#endif
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInDependency(XUAPluginData.Identifier, "4.16.0")] // need new APIs
    public partial class TranslationHelper
    {
        [PublicAPI]
        public const string GUID = Constants.PluginGUIDPrefix + "." + nameof(TranslationHelper);

        public const string PluginName = "Translation Helper";
        public const string Version = "1.1.0.9";

        internal static new ManualLogSource Logger;
        private static TranslationHelper _instance;

        private static readonly HitMissCounter FastTranslateFullNameStats =
            new HitMissCounter($"{typeof(TranslationHelper).PrettyTypeFullName()}.{nameof(TryFastTranslateFullName)}");

        private static readonly HitMissCounter TranslateNameStats =
            new HitMissCounter($"{typeof(TranslationHelper).PrettyTypeFullName()}.{nameof(TryTranslateName)}");

        private const string CardListTranslationCachingConfigName = "Enable Card List Translation Caching";

        public static TranslationHelper Instance =>
            _instance != null ? _instance : _instance = FindObjectOfType<TranslationHelper>();

        /// <summary>
        ///     There are times the ExtendedSave events don't fire for performance reasons (avoiding loading extended data).
        ///     Set this to true for times when we need the TranslationHelper events to fire (extended data will still not be
        ///     loaded) even in those situations.
        /// </summary>
        public static bool AlternateLoadEventsEnabled { get; set; } = false;

        public static bool IsShuttingDown => KoikatuAPI.IsQuitting;

        private static bool _treatUnknownAsGameMode;

        internal bool TreatUnknownAsGameMode
        {
            get => _treatUnknownAsGameMode;
            set
            {
                if (_treatUnknownAsGameMode == value) return;
                _treatUnknownAsGameMode = value;
                UpdateCurrentCardTranslationMode();
            }
        }

        internal static bool SplitNamesBeforeTranslate;

        internal static readonly TranslatorReplacementsManager
            RegistrationManager = new TranslatorReplacementsManager();

        internal static readonly CardNameTranslationManager CardNameManager = new CardNameTranslationManager();

        internal static readonly ICollection<GameMode> RegistrationGameModes =
            new HashSet<GameMode> { GameMode.MainGame };

        // space, middle dot (full/half-width), ideographic space
        public static readonly char[] SpaceSplitter = { ' ', '\u30FB', '\uFF65', '\u3000' };

        public static readonly string SpaceJoiner = SpaceSplitter[0].ToString();

        private static readonly SimpleLazy<string> ConfigDirectoryLoader =
            LazyConfigDirectory(Paths.ConfigPath, GUID);

        private static readonly SimpleLazy<string> ConfigNamePresetDirectoryLoader =
            LazyConfigDirectory(ConfigDirectory, $"{nameof(NamePresets)}");

        private static readonly SimpleLazy<string> TranslationNamePresetDirectoryLoader =
            LazyConfigDirectory(Paths.BepInExRootPath, "Translation", nameof(TranslationHelper), nameof(NamePresets));

        private static readonly SimpleLazy<IEqualityComparer<string>> NameStringComparerLoader =
            new SimpleLazy<IEqualityComparer<string>>(() =>
                new TrimmedStringComparer(SpaceSplitter));

        private readonly SimpleLazy<CharaFileInfoTranslationManager> _fileInfoTranslationManager =
            new SimpleLazy<CharaFileInfoTranslationManager>(() => new CharaFileInfoTranslationManager());

        private readonly SimpleLazy<Presets.Manager> _namePresetManager =
            new SimpleLazy<Presets.Manager>(() => new Presets.Manager());

        private readonly SimpleLazy<NameTranslator> _nameTranslator =
            new SimpleLazy<NameTranslator>(() => new NameTranslator());

        [UsedImplicitly]
        internal static bool ShowGivenNameFirst { get; private set; }

        public static string ConfigDirectory => ConfigDirectoryLoader.Value;
        public static string ConfigNamePresetDirectory => ConfigNamePresetDirectoryLoader.Value;
        public static string TranslationNamePresetDirectory => TranslationNamePresetDirectoryLoader.Value;

        [Obsolete]
        [UsedImplicitly]
        internal static bool UsingAlternateLoadEvents => !ExtendedSave.LoadEventsEnabled && AlternateLoadEventsEnabled;

        internal static IEqualityComparer<string> NameStringComparer => NameStringComparerLoader.Value;
        internal NameTranslator NameTranslator => _nameTranslator.Value;

        internal Presets.Manager NamePresetManager => _namePresetManager.Value;

        internal CharaFileInfoTranslationManager FileInfoTranslationManager => _fileInfoTranslationManager.Value;

        // GameMode as of last update to CurrentCardLoadTranslationMode
        internal GameMode CurrentGameMode { get; private set; } = GameMode.Unknown;

        internal CardLoadTranslationMode CurrentCardLoadTranslationMode { get; private set; } =
            CardLoadTranslationMode.Disabled;

        internal bool CurrentCardLoadTranslationEnabled =>
            !IsShuttingDown && CurrentCardLoadTranslationMode >= CardLoadTranslationMode.CacheOnly;

        public static ConfigEntry<bool> RegisterActiveCharacters { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> GameTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> StudioTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> MakerTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<bool> TranslateNameWithSuffix { get; private set; }
        public static ConfigEntry<string> NameTrimChars { get; private set; }
        public static ConfigEntry<bool> MakerSaveWithTranslatedNames { get; internal set; }
        public static ConfigEntry<bool> EnableCardListTranslationCaching { get; private set; }

        /// <summary>
        ///     Occurs when behavior changes in such a way that previous translations are likely no longer correct.
        ///     Caches should be cleared and UI should be updated if possible.
        ///     Will propagate to CardTranslationBehaviorChanged and AccelerationBehaviorChanged
        /// </summary>
        [PublicAPI]
        public static event EventHandler<EventArgs> BehaviorChanged;

        /// <summary>
        ///     Occurs when behavior changes for card translation in such a way that previous translations are likely no longer
        ///     correct.
        ///     Caches should be cleared and UI should be updated if possible.
        /// </summary>
        [PublicAPI]
        public static event EventHandler<EventArgs> CardTranslationBehaviorChanged;

        /// <summary>
        ///     Occurs when behavior changes for acceleration in such a way that previous translations are likely no longer
        ///     correct.
        ///     Caches should be cleared and UI should be updated if possible.
        /// </summary>
        [PublicAPI]
        public static event EventHandler<EventArgs> AccelerationBehaviorChanged;

        internal static Dictionary<string, string> StringCacheInitializer()
        {
            return new Dictionary<string, string>(NameStringComparer);
        }

        internal static Dictionary<string, string> PathCacheInitializer()
        {
            return new Dictionary<string, string>(PathUtils.NormalizedPathComparer);
        }

        internal static SimpleLazy<string> LazyConfigDirectory(params string[] paths)
        {
            string Init()
            {
                var configDir = PathUtils.CombinePaths(paths);
                if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
                return configDir;
            }

            return new SimpleLazy<string>(Init);
        }

        internal static void NotifyBehaviorChanged(EventArgs e)
        {
            Instance.SafeProc(i => i.OnBehaviorChanged(e));
        }

        internal static void NotifyCardTranslationBehaviorChanged(EventArgs e)
        {
            Instance.SafeProc(i => i.OnCardTranslationBehaviorChanged(e));
        }

        internal static void NotifyAccelerationBehaviourChanged(EventArgs e)
        {
            Instance.SafeProc(i => i.OnAccelerationBehaviorChanged(e));
        }

        protected void OnBehaviorChanged(EventArgs e)
        {
            Logger.DebugLogDebug($"{nameof(OnCardTranslationBehaviorChanged)}({e})");
            NotifyCardTranslationBehaviorChanged(e);
            NotifyAccelerationBehaviourChanged(e);
            BehaviorChanged?.SafeInvoke(this, e);
        }

        protected void OnCardTranslationBehaviorChanged(EventArgs e)
        {
            Logger.DebugLogDebug($"{nameof(OnCardTranslationBehaviorChanged)}({e})");
            CardTranslationBehaviorChanged?.SafeInvoke(this, e);
        }

        protected void OnAccelerationBehaviorChanged(EventArgs e)
        {
            Logger.DebugLogDebug($"{nameof(OnCardTranslationBehaviorChanged)}({e})");
            AccelerationBehaviorChanged?.SafeInvoke(this, e);
        }


        internal void UpdateCurrentCardTranslationMode()
        {
            var origMode = CurrentCardLoadTranslationMode;
            if (StudioAPI.InsideStudio)
            {
                CurrentCardLoadTranslationMode =
                    StudioTranslateCardNameOnLoad?.Value ?? CardLoadTranslationMode.Disabled;
                CurrentGameMode = GameMode.Studio;
            }

            else if (MakerAPI.InsideMaker)
            {
                CurrentCardLoadTranslationMode =
                    MakerTranslateCardNameOnLoad?.Value ?? CardLoadTranslationMode.Disabled;
                CurrentGameMode = GameMode.Maker;
            }
            else
            {
                var mode = KoikatuAPI.GetCurrentGameMode();
                CurrentGameMode = mode == GameMode.Unknown && TreatUnknownAsGameMode ? GameMode.MainGame : mode;

                if (CurrentGameMode == GameMode.MainGame)
                {
                    CurrentCardLoadTranslationMode =
                        GameTranslateCardNameOnLoad?.Value ?? CardLoadTranslationMode.Disabled;
                }
                else
                {
                    CurrentCardLoadTranslationMode = CardLoadTranslationMode.Disabled;
                }
            }

            if (IsShuttingDown) CurrentCardLoadTranslationMode = CardLoadTranslationMode.Disabled;

            if (origMode == CurrentCardLoadTranslationMode || (
                    origMode != CardLoadTranslationMode.Disabled &&
                    CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled))
            {
                return;
            }

            Logger.LogDebug($"UpdateCurrentCardTranslationMode: {origMode} => {CurrentCardLoadTranslationMode}");
            OnCardTranslationBehaviorChanged(EventArgs.Empty);
        }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;

            SplitNamesBeforeTranslate = false;
            RegisterActiveCharacters = Config.Bind("Translate Card Name Options", "Register Active Characters", true,
                "Register active character names as replacements with translator");
            TranslateNameWithSuffix = Config.Bind("Translate Card Name Options", "Use Suffix", false,
                "Append suffix to names before translating to send hint they are names");
            NameTrimChars = Config.Bind("Translate Card Name Options", "Characters to Trim", string.Empty,
                "Characters to trim from returned translations");

            GameTranslateCardNameOnLoad =
                InitializeGameModeConfig(GameMode.MainGame, CardLoadTranslationMode.CacheOnly);
            MakerTranslateCardNameOnLoad = InitializeGameModeConfig(GameMode.Maker, CardLoadTranslationMode.CacheOnly);
            StudioTranslateCardNameOnLoad =
                InitializeGameModeConfig(GameMode.Studio, CardLoadTranslationMode.CacheOnly);

            MakerSaveWithTranslatedNames = Config.Bind("Maker", "Save Translated Names", false,
                "When enabled translated names will be saved with cards in maker, otherwise unmodified names will be restored");

            if (!StudioAPI.InsideStudio)
            {
                MakerAPI.InsideMakerChanged += InsideMakerChanged;
                SceneManager.activeSceneChanged += ActiveSceneChanged;
            }


            EnableCardListTranslationCaching = Config.Bind("Advanced", CardListTranslationCachingConfigName,
                GetEnableCardListTranslationCachingDefault(),
                new ConfigDescription(
                    "Enables caching to prevent delays when encountering files multiple times. Greatly speeds up browsing cards. If another plugin is doing something non-standard and you see duplicate names, try disabling.",
                    null, "Advanced"));

            KoikatuAPI.Quitting += ApplicationQuitting;
            UpdateCurrentCardTranslationMode();
        }

        private void ApplicationQuitting(object sender, EventArgs e)
        {
            LogCacheStats(nameof(ApplicationQuitting));
        }


#if !KK
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification =
            "not static for all implementations")]
        private bool GetEnableCardListTranslationCachingDefault()
        {
            return true;
        }
#endif

        private void ActiveSceneChanged(Scene arg0, Scene arg1)
        {
            UpdateCurrentCardTranslationMode();
        }

        private void InsideMakerChanged(object sender, EventArgs e)
        {
            UpdateCurrentCardTranslationMode();
        }


        private ConfigEntry<CardLoadTranslationMode> InitializeGameModeConfig(GameMode mode,
            CardLoadTranslationMode defaultValue)
        {
            var config = Config.Bind("Translate Card Name Modes", mode.ToString(), defaultValue,
                $"Attempt to translate card names when they are loaded in {mode}");
            config.SettingChanged += (o, e) => UpdateCurrentCardTranslationMode();
            return config;
        }

        internal void Awake()
        {
            _instance = this;
            Logger = Logger ?? base.Logger;

            Configuration.Setup();
            Acceleration.Configuration.Setup();
            Chara.Configuration.Setup();
            if (StudioAPI.InsideStudio)
            {
                Studio.Configuration.Setup();
            }
            else
            {
                MainGame.Configuration.Setup();
                Maker.Configuration.Setup();
            }

            GameSpecificAwake();
            GeBoAPI.TranslationsLoaded += GeBoAPI_TranslationsLoaded;
        }

        private static void GeBoAPI_TranslationsLoaded(object sender, EventArgs eventArgs)
        {
            NotifyBehaviorChanged(eventArgs);
        }

        internal void OnDestroy()
        {
            CurrentCardLoadTranslationMode = CardLoadTranslationMode.Disabled;
            RegistrationManager.Deactivate();
        }

        internal void Start()
        {
            _instance = this;
            Logger = Logger ?? base.Logger;
            GameSpecificStart();
        }

        [PublicAPI]
        public static IEnumerator WaitOnCardTranslations()
        {
            return CardNameManager.WaitOnCardTranslations();
        }

        public static IEnumerator WaitOnCard(ChaFile file)
        {
            return CardNameManager.WaitOnCard(file);
        }

        [PublicAPI]
        public static IEnumerator TranslateCardNames(ChaFile file)
        {
            return CardNameManager.TranslateCardNames(file);
        }

        [UsedImplicitly]
        internal static IEnumerator TranslateFileInfo(ICharaFileInfo fileInfo,
            params TranslationResultHandler[] callbacks)
        {
            return Instance.FileInfoTranslationManager.TranslateFileInfo(fileInfo, callbacks);
        }

#if false
        // handled by controller now
        internal void ExtendedSave_CardBeingLoaded(ChaFile file)
        {
            if (file != null && CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
            {
                file.StartMonitoredCoroutine(CardNameManager.TranslateCardNames(file));
            }
        }

        private void CharacterApi_CharacterReloaded(object dummy, CharaReloadEventArgs e)
        {
            if (e.ReloadedCharacter != null && e.ReloadedCharacter.chaFile != null &&
                CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
            {
                e.ReloadedCharacter.StartMonitoredCoroutine(
                    CardNameManager.TranslateCardNames(e.ReloadedCharacter.chaFile));
            }
        }
#endif

        internal void AddTranslatedNameToCache(string origName, string translatedName, bool allowPersistToDisk = false)
        {
            if (NameStringComparer.Equals(origName, translatedName) ||
                StringUtils.ContainsJapaneseChar(translatedName))
            {
                return;
            }

            // only persist if network translation enabled
            var persistToDisk =
                allowPersistToDisk && CurrentCardLoadTranslationMode > CardLoadTranslationMode.CacheOnly;

            GeBoAPI.Instance.AutoTranslationHelper.AddTranslationToCache(origName, translatedName, persistToDisk, 0x01,
                -1);
        }

        internal IEnumerator RegisterReplacements(ChaFile file)
        {
            if (file == null) yield break;
            //Logger.LogDebug($"RegisterReplacements {file} {file.parameter.fullname}");
            if (!RegistrationGameModes.Contains(CurrentGameMode)) yield break;

            yield return file.StartMonitoredCoroutine(CardNameManager.WaitOnCard(file));
            if (RegistrationManager.IsTracked(file))
            {
                if (!RegistrationManager.HaveNamesChanged(file)) yield break;
                RegistrationManager.Untrack(file);
            }

            RegistrationManager.Track(file);
        }

        internal IEnumerator RegisterReplacementsWrapper(ChaFile file, bool alreadyTranslated = false)
        {
            if (file == null) yield break;
            // handle card translation BEFORE registering replacements
            if (!alreadyTranslated && CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
            {
                file.StartMonitoredCoroutine(CardNameManager.TranslateCardNames(file));
                yield return null;
            }

            //StartCoroutine(RegisterReplacements(file));
            /*yield return*/
            file.StartMonitoredCoroutine(RegisterReplacements(file));
        }

        internal IEnumerator UnregisterReplacements(ChaFile file)
        {
            if (file == null || IsShuttingDown) yield break;
            //if (!RegistrationGameModes.Contains(CurrentGameMode)) yield break;
            yield return null;
            RegistrationManager.Untrack(file);
            var _ = this;
        }

        [PublicAPI]
        public static bool TryFastTranslateFullName(ICharaFileInfo fileInfo, out string translatedName)
        {
            return TryFastTranslateFullName(new NameScope(fileInfo.Sex), fileInfo.Name, fileInfo.FullPath,
                out translatedName);
        }

        public static bool TryFastTranslateFullName(NameScope scope, string originalName, out string translatedName)
        {
            if (originalName.IsNullOrWhiteSpace())
            {
                translatedName = originalName;
                return true;
            }

            if (Instance.NamePresetManager.TryTranslateFullName(scope.Sex, originalName, out translatedName) ||
                CardNameTranslationManager.TryGetRecentTranslation(scope, originalName, out translatedName))
            {
                FastTranslateFullNameStats.RecordHit();
                return true;
            }

            FastTranslateFullNameStats.RecordMiss();
            return false;
        }

        public static bool TryFastTranslateFullName(NameScope scope, string originalName, string path,
            out string translatedName)
        {
            if (path != null && !originalName.IsNullOrEmpty() &&
                CharaFileInfoTranslationManager.TryGetRecentTranslation(scope, path, out translatedName))
            {
                FastTranslateFullNameStats.RecordHit();
                return true;
            }

            return TryFastTranslateFullName(scope, originalName, out translatedName);
        }

        public static bool TryTranslateName(NameScope scope, string originalName, out string translatedName)
        {
            if (originalName.IsNullOrWhiteSpace())
            {
                translatedName = originalName;
                return true;
            }

            if (CardNameTranslationManager.TryGetRecentTranslation(scope, originalName, out translatedName) ||
                Instance.NameTranslator.TryTranslateName(originalName, scope, out translatedName))
            {
                TranslateNameStats.RecordHit();
                return true;
            }

            TranslateNameStats.RecordMiss();
            return false;
        }

        [PublicAPI]
        public static bool TryTranslateName(NameScope scope, string originalName, string path,
            out string translatedName)
        {
            if (!path.IsNullOrEmpty() && !originalName.IsNullOrEmpty() &&
                CharaFileInfoTranslationManager.TryGetRecentTranslation(scope, path, out translatedName))

            {
                TranslateNameStats.RecordHit();
                return true;
            }

            return TryTranslateName(scope, originalName, out translatedName);
        }


        public static bool NameNeedsTranslation(string name, NameScope scope)
        {
            return CardNameTranslationManager.NameNeedsTranslation(name, scope);
        }

        private static void LogCacheStats(string prefix)
        {
            Logger?.LogDebug(FastTranslateFullNameStats.GetCounts(prefix));
            Logger?.LogDebug(TranslateNameStats.GetCounts(prefix));
        }
    }
}
