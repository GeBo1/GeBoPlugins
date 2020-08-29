using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using GeBoCommon.Utilities;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Translation;
using UnityEngine.SceneManagement;
using Configuration = TranslationHelperPlugin.Translation.Configuration;
using PluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;
#if AI || HS2
using AIChara;

#endif

namespace TranslationHelperPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInDependency(PluginData.Identifier)]
    public partial class TranslationHelper
    {
        public const string GUID = "com.gebo.bepinex.translationhelper";
        public const string PluginName = "Translation Helper";
        public const string Version = "0.9.4";

        internal static new ManualLogSource Logger;
        public static TranslationHelper Instance;

        /// <summary>
        ///     There are times the ExtendedSave events don't fire for performance reasons (avoiding loading extended data).
        ///     Set this to true for times when we need the TranslationHelper events to fire (extended data will still not be
        ///     loaded)
        ///     even in those situations.
        /// </summary>
        public static bool AlternateLoadEventsEnabled = false;

        internal static bool SplitNamesBeforeTranslate;

        internal static readonly TranslatorReplacementsManager
            RegistrationManager = new TranslatorReplacementsManager();

        internal static readonly CardNameTranslationManager CardNameManager = new CardNameTranslationManager();

        internal static ICollection<GameMode> RegistrationGameModes =
            new HashSet<GameMode> {GameMode.MainGame};

        // space, middle dot (full/half-width), ideographic space
        public static readonly char[] SpaceSplitter = {' ', '\u30FB', '\uFF65', '\u3000'};

        public static readonly string SpaceJoiner = SpaceSplitter[0].ToString();

        private readonly SimpleLazy<NameTranslator> _nameTranslator =
            new SimpleLazy<NameTranslator>(() => new NameTranslator());

        internal static bool UsingAlternateLoadEvents => !ExtendedSave.LoadEventsEnabled && AlternateLoadEventsEnabled;


        internal NameTranslator NameTranslator => _nameTranslator.Value;

        // GameMode as of last update to CurrentCardLoadTranslationMode
        internal GameMode CurrentGameMode { get; private set; } = GameMode.Unknown;
        internal CardLoadTranslationMode CurrentCardLoadTranslationMode { get; private set; } =
            CardLoadTranslationMode.Disabled;

        internal bool CurrentCardLoadTranslationEnabled =>
            CurrentCardLoadTranslationMode >= CardLoadTranslationMode.CacheOnly;

        public static ConfigEntry<bool> RegisterActiveCharacters { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> GameTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> StudioTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> MakerTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<bool> TranslateNameWithSuffix { get; private set; }
        public static ConfigEntry<string> NameTrimChars { get; private set; }
        public static ConfigEntry<bool> MakerSaveWithTranslatedNames { get; internal set; }

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
                CurrentGameMode = KoikatuAPI.GetCurrentGameMode();

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


            Logger.LogDebug($"UpdateCurrentCardTranslationMode: {origMode} => {CurrentCardLoadTranslationMode}");
        }

        internal void Main()
        {
            Instance = this;
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

            UpdateCurrentCardTranslationMode();
        }

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
            Instance = this;
            Logger = Logger ?? base.Logger;

            Configuration.Setup();
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
        }


        internal void OnDestroy()
        {
            RegistrationManager.Deactivate();
        }

        internal void Start()
        {
            GameSpecificStart();
        }

        public static IEnumerator WaitOnCardTranslations()
        {
            return CardNameManager.WaitOnCardTranslations();
        }

        public static IEnumerator WaitOnCard(ChaFile file)
        {
            return CardNameManager.WaitOnCard(file);
        }

        public static IEnumerator TranslateCardNames(ChaFile file)
        {
            return CardNameManager.TranslateCardNames(file);
        }

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

        internal void AddTranslatedNameToCache(string origName, string translatedName, bool allowPersistToDisk = false)
        {
            if (origName == translatedName || StringUtils.ContainsJapaneseChar(translatedName)) return;
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

            yield return CardNameManager.WaitOnCard(file);
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
            if (file == null) yield break;
            //if (!RegistrationGameModes.Contains(CurrentGameMode)) yield break;
            yield return null;
            RegistrationManager.Untrack(file);
        }
    }
}
