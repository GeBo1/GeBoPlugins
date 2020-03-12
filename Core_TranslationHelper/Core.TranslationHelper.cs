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

        internal static new ManualLogSource Logger;
        public static TranslationHelper Instance;

        #region ConfigMgr

        public static ConfigEntry<bool> RegisterActiveCharacters { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> GameTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> StudioTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<CardLoadTranslationMode> MakerTranslateCardNameOnLoad { get; private set; }
        public static ConfigEntry<bool> TranslateNameWithSuffix { get; private set; }
        public static ConfigEntry<string> NameTrimChars { get; private set; }

        #endregion ConfigMgr

        internal static bool SplitNamesBeforeTranslate;

        private static readonly TranslatorReplacementsManager RegistrationManager = new TranslatorReplacementsManager();
        internal static CardNameTranslationManager CardNameManager = new CardNameTranslationManager();
        private static readonly ICollection<GameMode> RegistrationGameModes = new HashSet<GameMode> { GameMode.MainGame, GameMode.Studio };

        internal static readonly char[] SpaceSplitter = new char[] { ' ' };

        internal void Main()
        {
            Instance = this;
            Logger = base.Logger;

            SplitNamesBeforeTranslate = false;
            RegisterActiveCharacters = Config.Bind("Config", "Register Active Characters", true, "Register active character names as replacements with translator");
            TranslateNameWithSuffix = Config.Bind("Translate Card Name Options", "Use Suffix", false, "Append suffix to names before translating to send hint they are names");
            NameTrimChars = Config.Bind("Translate Card Name Options", "Characters to Trim", string.Empty, "Characters to trim from returned translations");

            GameTranslateCardNameOnLoad = InitializeGameModeConfig(GameMode.MainGame, CardLoadTranslationMode.Disabled);
            MakerTranslateCardNameOnLoad = InitializeGameModeConfig(GameMode.Maker, CardLoadTranslationMode.Disabled);
            StudioTranslateCardNameOnLoad = InitializeGameModeConfig(GameMode.Studio, CardLoadTranslationMode.CacheOnly);
        }

        private ConfigEntry<CardLoadTranslationMode> InitializeGameModeConfig(GameMode mode, CardLoadTranslationMode defaultValue)
        {
            return Config.Bind("Translate Card Name Modes", mode.ToString(), defaultValue, $"Attempt to translate card names when they are loaded in {mode}");
        }

        internal void Awake()
        {
            HarmonyWrapper.PatchAll(typeof(TranslationHelper));
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

        private void ExtendedSave_CardBeingLoaded(ChaFile file)
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

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
        public static void ChaControl_Reload(ChaControl __instance)
        {
            if (RegisterActiveCharacters.Value)
            {
                Instance.StartCoroutine(Instance.RegisterReplacementsWrapper(__instance?.chaFile));
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Load))]
        public static void ChaControl_Load(ChaControl __instance)
        {
            if (RegisterActiveCharacters.Value)
            {
                Instance.StartCoroutine(Instance.RegisterReplacementsWrapper(__instance?.chaFile));
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.OnDestroy))]
        public static void ChaControl_OnDestroy(ChaControl __instance)
        {
            if (Instance)
            {
                Instance.StartCoroutine(Instance.UnregisterReplacements(__instance?.chaFile));
            }
        }

        internal void AddTranslatedNameToCache(string origName, string translatedName)
        {
            GeBoAPI.Instance.AutoTranslationHelper.AddTranslationToCache(origName, translatedName, true, 0x01, -1);
        }

        internal IEnumerator RegisterReplacements(ChaFile file)
        {
            if (file != null)
            {
                if (RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode()) && !RegistrationManager.IsTracked(file))
                {
                    yield return CardNameManager.WaitOnCard(file);
                    RegistrationManager.Track(file);
                }
            }
        }

        internal IEnumerator RegisterReplacementsWrapper(ChaFile file)
        {
            if (file != null)
            {
                // handle card translation BEFORE registering replacements
                if (CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
                {
                    StartCoroutine(CardNameManager.TranslateCardNames(file));
                    yield return null;
                }

                //StartCoroutine(RegisterReplacements(file));
                yield return
                StartCoroutine(KKAPI.Utilities.CoroutineUtils.ComposeCoroutine(
                    CardNameManager.WaitOnCard(file),
                    RegisterReplacements(file)));
            }
        }

        internal IEnumerator UnregisterReplacements(ChaFile file)
        {
            if (file != null)
            {
                if (RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode()))
                {
                    yield return null;
                    RegistrationManager.Untrack(file);
                }
            }
        }

#if false
        private string CleanTranslationResult(string input, string suffix)
        {
            return string.Join(" ", input.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries).Select((s) => CleanTranslationResultSection(s, suffix)).ToArray());
        }

        private string CleanTranslationResultSection(string input, string suffix)
        {
            string output = input;
            string preClean = string.Empty;
            //Logger.LogDebug($"Cleaning: '{input}'");
            while (preClean != output)
            {
                preClean = output;
                if (!suffix.IsNullOrEmpty())
                {
                    foreach (string format in SuffixFormats)
                    {
                        string testSuffix = string.Format(format, suffix);
                        if (output.TrimEnd().EndsWith(testSuffix))
                        {
                            //Logger.LogDebug($"Cleaning: '{input}': suffix match: {testSuffix} <= '{output}'");
                            output = output.Remove(output.LastIndexOf(testSuffix)).TrimEnd();
                            //Logger.LogDebug($"Cleaning: '{input}': suffix match: {testSuffix} => '{output}'");
                            break;
                        }
                    }
                }
                output = output.Trim().Trim(NameTrimChars.Value.ToCharArray()).Trim();
                //Logger.LogDebug($"Cleaning: '{input}': trimmed: => '{output}");
            }
            //Logger.LogDebug($"Cleaning: '{input}': result: '{output}'");
            return output;
        }

        private void TranslateName(string name, byte gender, Action<string> onCompleted)
        {
            string origName = name;
            string suffixedName = origName;

            KeyValuePair<string, string> activeSuffix = new KeyValuePair<string, string>(string.Empty, string.Empty);

            if (CurrentCardLoadTranslationMode >= CardLoadTranslationMode.CacheOnly)
            {
                //Logger.LogDebug($"TranslateName: attempting translation (cached): {origName}");
                if (AutoTranslator.Default.TryTranslate(origName, out string translatedName))
                {
                    translatedName = CleanTranslationResult(translatedName, activeSuffix.Value);
                    if (origName != translatedName)
                    {
                        //Logger.LogInfo($"TranslateName: Translated card name: {origName} -> {translatedName}");
                        onCompleted(translatedName);
                        return;
                    }
                }

                if (TranslateNameWithSuffix.Value)
                {
                    activeSuffix = Suffixes[gender];
                    suffixedName = string.Join("", new string[] { origName, activeSuffix.Key });
                    //Logger.LogDebug($"TranslateName: attempting translation (cached): {suffixedName}");
                    if (AutoTranslator.Default.TryTranslate(suffixedName, out string translatedName2))
                    {
                        translatedName2 = CleanTranslationResult(translatedName2, activeSuffix.Value);
                        if (suffixedName != translatedName2)
                        {
                            //Logger.LogInfo($"TranslateName: Translated card name: {origName} -> {translatedName2}");
                            onCompleted(translatedName2);
                            return;
                        }
                    }
                }
            }

            if (CurrentCardLoadTranslationMode == CardLoadTranslationMode.FullyEnabled)
            {
                // suffixedName will be origName if TranslateNameWithSuffix is off, so just use it here
                //Logger.LogDebug($"TranslateName: attempting translation (async): {suffixedName}");
                AutoTranslator.Default.TranslateAsync(suffixedName, (result) =>
                {
                    if (result.Succeeded)
                    {
                        string translatedName = CleanTranslationResult(result.TranslatedText, activeSuffix.Value);
                        if (suffixedName != translatedName)
                        {
                            //Logger.LogInfo($"TranslateName: Translated card name: {origName} -> {translatedName}");
                            onCompleted(translatedName);
                            return;
                        }
                    }
                    onCompleted(string.Empty);
                });
            }
            else
            {
                onCompleted(string.Empty);
            }
        }

        public IEnumerator TranslateCardName(ChaFile file)
        {
            string charaFileName = file.charaFileName;
            byte gender = file.parameter.sex;

            //Logger.LogDebug($"TranslateCardName: attempting to translated name(s): {charaFileName}");

            int jobs = 0;
            foreach (KeyValuePair<int, string> name in file.IterNames())
            {
                string origName = name.Value;
                int nameKey = name.Key;

                if (origName.IsNullOrEmpty() || !LanguageHelper.IsTranslatable(origName) || !ContainsJapaneseCharRegex.IsMatch(origName))
                {
                    continue;
                }

                // check the cache with raw name to start
                if (AutoTranslator.Default.TryTranslate(name.Value, out string cachedName))
                {
                    if (origName != cachedName)
                    {
                        //Logger.LogInfo($"TranslateCardName: Translated card name (from cache): {charaFileName}[{nameKey}]:  {origName} -> {cachedName}");
                        file.SetName(nameKey, cachedName);
                        continue;
                    }
                }

                // not cached already
                Interlocked.Increment(ref jobs);

                //Logger.LogDebug($"TranslateCardName: attempting to translate: {charaFileName}[{nameKey}]: {origName}");
                List<string> names = new List<string>(new string[] { name.Value });
                int remain = 0;

                if (SplitNamesBeforeTranslate)
                {
                    names = new List<string>(name.Value.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries));
                }

                Interlocked.Add(ref remain, names.Count);

                int namePart = 0;
                bool changed = false;
                foreach (string subName in names.ToArray())
                {
                    int resultPart = namePart;
                    namePart++;

                    //Logger.LogDebug($"TranslateCardName: attempting to translate (subName): {charaFileName}[{nameKey}][{resultPart}]: {subName}");

                    TranslateName(subName, gender, (result) =>
                    {
                        if (!result.IsNullOrEmpty())
                        {
                            //Logger.LogDebug($"TranslateCardName: partial name translation: {charaFileName}[{nameKey}][{resultPart}]: {names[resultPart]} => {result}");
                            names[resultPart] = result;
                            changed = true;
                        }

                        Interlocked.Decrement(ref remain);

                        if (remain == 0)
                        {
                            if (changed)
                            {
                                string translatedName = names.Join(delimiter: " ");
                                if (translatedName != origName)
                                {
                                    //Logger.LogInfo($"TranslateCardName: Translated card name: {charaFileName}[{nameKey}]: {origName} -> {translatedName}");
                                    file.SetName(nameKey, translatedName);
                                    if (CurrentCardLoadTranslationMode > CardLoadTranslationMode.CacheOnly)
                                    {
                                        AddTranslatedNameToCache(origName, translatedName);
                                    }
                                }
                                else
                                {
                                    changed = false;
                                }
                            }

                            if (!changed)
                            {
                                //Logger.LogDebug($"TranslateCardName: Translated card name unchanged: {charaFileName}[{nameKey}]: {origName}");
                            }

                            Interlocked.Decrement(ref jobs);
                        }
                    });
                }
                yield return null;
            }
            int count = 0;
            while (jobs > 0)
            {
                if (count == 0)
                {
                    //Logger.LogDebug($"TranslateCardName: waiting for translation jobs to finish: {charaFileName}: {jobs}");
                }
                count = (count + 1) % 100;
                yield return null;
            }
            //Logger.LogDebug($"TranslateCardName: translation complete: {charaFileName}");
        }
#endif
    }
}
