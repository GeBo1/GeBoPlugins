using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using UnityEngine.SceneManagement;
using GeBoCommon.Utilities;
#if AI
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier)]
    public partial class TranslationHelper
    {
        public const string GUID = "com.gebo.bepinex.translationhelper";
        public const string PluginName = "Translation Helper";
        public const string Version = "0.8.0";

        private readonly SimpleLazy<NameTranslator> _nameTransator = new SimpleLazy<NameTranslator>(() => new NameTranslator());

        internal static new ManualLogSource Logger;
        public static TranslationHelper Instance;

        #region ConfigMgr

        public static ConfigEntry<bool> RegisterActiveCharacters { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> GameTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> StudioTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> MakerTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<bool> TranslateNameWithSuffix { get; private set; }
        public static ConfigEntry<string> NameTrimChars { get; private set; }

        public static ConfigEntry<bool> EnableOverrideNameTranslationScope { get; private set; }
        public static ConfigEntry<int> OverrideNameTranslationScope { get; private set; }


        #endregion ConfigMgr

        internal static bool SplitNamesBeforeTranslate;

        private static readonly TranslatorReplacementsManager RegistrationManager = new TranslatorReplacementsManager();
        internal static CardNameTranslationManager CardNameManager = new CardNameTranslationManager();
        private static readonly ICollection<GameMode> RegistrationGameModes = new HashSet<GameMode> { GameMode.MainGame, GameMode.Studio };

        internal static readonly char[] SpaceSplitter = new char[] { ' ' };

        internal NameTranslator NameTranslator => _nameTransator.Value;

        internal void Main()
        {
            Instance = this;
            Logger = Logger ?? base.Logger;

            SplitNamesBeforeTranslate = false;
            RegisterActiveCharacters = Config.Bind("Config", "Register Active Characters", true,
                "Register active character names as replacements with translator");
            TranslateNameWithSuffix = Config.Bind("Translate Card Name Options", "Use Suffix", false,
                "Append suffix to names before translating to send hint they are names");
            NameTrimChars = Config.Bind("Translate Card Name Options", "Characters to Trim", string.Empty,
                "Characters to trim from returned translations");

            GameTranslateCardNameOnLoad = InitializeGameModeConfig(GameMode.MainGame, CardLoadTranslationMode.Disabled);
            MakerTranslateCardNameOnLoad = InitializeGameModeConfig(GameMode.Maker, CardLoadTranslationMode.Disabled);
            StudioTranslateCardNameOnLoad =
                InitializeGameModeConfig(GameMode.Studio, CardLoadTranslationMode.CacheOnly);

            EnableOverrideNameTranslationScope = Config.Bind("Override Translation Scope", "Enable", true,
                "Attempt to translate names with a specific scope active");
            OverrideNameTranslationScope = Config.Bind("Override Translation Scope", "Scope", 9999,
                "Scope ID to use (should not already be in use by game)");
        }

        private ConfigEntry<CardLoadTranslationMode> InitializeGameModeConfig(GameMode mode,
            CardLoadTranslationMode defaultValue)
        {
            return Config.Bind("Translate Card Name Modes", mode.ToString(), defaultValue,
                $"Attempt to translate card names when they are loaded in {mode}");
        }

        internal void Awake()
        {
            Instance = this;
            Logger = Logger ?? base.Logger;
            Hooks.SetupHooks();

            GameSpecificAwake();


        }

        internal void OnDestroy()
        {
            RegistrationManager.Reset();
        }

        internal void Start()
        {
            ExtendedSave.CardBeingLoaded += ExtendedSave_CardBeingLoaded;
            CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            GameSpecificStart();
        }

        internal CardLoadTranslationMode CurrentCardLoadTranslationMode
        {
            get
            {
                switch (KoikatuAPI.GetCurrentGameMode())
                {
                    case GameMode.MainGame:
                        return GameTranslateCardNameOnLoad.Value;

                    case GameMode.Maker:
                        return MakerTranslateCardNameOnLoad.Value;

                    case GameMode.Studio:
                        return StudioTranslateCardNameOnLoad.Value;

                    default:
                        return CardLoadTranslationMode.Disabled;
                }
            }
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
                StartCoroutine(CardNameManager.TranslateCardNames(file));
            }
        }

        private void CharacterApi_CharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            if (e.ReloadedCharacter?.chaFile != null && CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
            {
                StartCoroutine(CardNameManager.TranslateCardNames(e.ReloadedCharacter?.chaFile));
            }
        }

        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode()))
            {
                RegistrationManager.Cleanup();
            }
            else
            {
                RegistrationManager.Reset();
            }
        }

        

        internal void AddTranslatedNameToCache(string origName, string translatedName)
        {
            GeBoAPI.Instance.AutoTranslationHelper.AddTranslationToCache(origName, translatedName, true, 0x01, -1);
        }

        internal IEnumerator RegisterReplacements(ChaFile file)
        {
            if (file == null) yield break;
            if (!RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode()) ||
                RegistrationManager.IsTracked(file)) yield break;
            yield return CardNameManager.WaitOnCard(file);
            RegistrationManager.Track(file);
        }

        internal IEnumerator RegisterReplacementsWrapper(ChaFile file)
        {
            if (file == null) yield break;
            // handle card translation BEFORE registering replacements
            if (CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
            {
                StartCoroutine(CardNameManager.TranslateCardNames(file));
                yield return null;
            }

            //StartCoroutine(RegisterReplacements(file));
            yield return StartCoroutine(KKAPI.Utilities.CoroutineUtils.ComposeCoroutine(
                CardNameManager.WaitOnCard(file),
                RegisterReplacements(file)));
        }

        internal IEnumerator UnregisterReplacements(ChaFile file)
        {
            if (file == null) yield break;
            if (!RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode())) yield break;
            yield return null;
            RegistrationManager.Untrack(file);
        }
    }
}
