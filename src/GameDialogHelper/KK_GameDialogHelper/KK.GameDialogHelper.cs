using System;
using System.Collections.Generic;
using ActionGame.Communication;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GameDialogHelperPlugin.PluginModeLogic;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using UnityEngine.SceneManagement;

namespace GameDialogHelperPlugin
{
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInProcess(Constants.MainGameProcessNameVR)]
    [BepInProcess(Constants.MainGameProcessNameVRSteam)]
    public partial class GameDialogHelper : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.GameDialogHelper";
        public const string PluginName = "Game Dialog Helper";
        public const string Version = "0.9.1";

        internal static new ManualLogSource Logger;
        internal static GameDialogHelper Instance;

        private static readonly SimpleLazy<InfoCheckSelectConditionsDelegate> InfoCheckSelectConditionsLoader =
            new SimpleLazy<InfoCheckSelectConditionsDelegate>(() =>
            {
                var csc = AccessTools.Method(typeof(Info), "CheckSelectConditions");
                return (InfoCheckSelectConditionsDelegate) Delegate.CreateDelegate(
                    typeof(InfoCheckSelectConditionsDelegate), csc);
            });

        private static readonly string[] Splitter = {",tag"};
        private static SaveData.Heroine _targetHeroine;

        private static IPluginModeLogic _logic;

        private readonly HashSet<string> _supportedSceneNames = new HashSet<string>(new[] {"Talk"});
        internal static DialogInfo CurrentDialog { get; private set; }

        public static ConfigEntry<PluginMode> CurrentPluginMode { get; private set; }
        public static ConfigEntry<RelationshipLevel> MinimumRelationshipLevel { get; private set; }
        public static ConfigEntry<string> CorrectHighlight { get; private set; }
        public static ConfigEntry<string> IncorrectHighlight { get; private set; }
        public static bool CurrentlyEnabled { get; private set; }

        private static IAutoTranslationHelper AutoTranslator => GeBoAPI.Instance.AutoTranslationHelper;

        public static SaveData.Heroine TargetHeroine
        {
            get
            {
                if (!CurrentlyEnabled) return null;
                if (_targetHeroine == null) FindObjectOfType<TalkScene>().SafeProcObject(
                        ts => _targetHeroine = ts.targetHeroine);
                return _targetHeroine;
            }
        }

        internal static InfoCheckSelectConditionsDelegate InfoCheckSelectConditions =>
            InfoCheckSelectConditionsLoader.Value;

        internal static void SetCurrentDialog(int questionId, int correctAnswerId, int numChoices)
        {
            CurrentDialog = new DialogInfo(questionId, correctAnswerId, numChoices);
        }

        internal static void ClearCurrentDialog()
        {
            CurrentDialog = null;
        }

        public void Main()
        {
            Instance = this;
            Logger = base.Logger;

            CurrentPluginMode = Config.Bind("Config", "Plugin Mode", PluginMode.RelationshipBased,
                "Controls how plugin operates");

            CorrectHighlight = Config.Bind("General", "Highlight (correct)", "←",
                "String to append to highlighting correct answers");
            IncorrectHighlight = Config.Bind("General", "Highlight (incorrect)", "∅",
                "String to append when highlighting incorrect answers");

            MinimumRelationshipLevel = Config.Bind("Relationship Mode", "Minimum Relationship",
                RelationshipLevel.Friend,
                "Highlight correct choice if relationship with character is the selected level or higher");

            CurrentPluginMode.SettingChanged += CurrentPluginMode_SettingChanged;

            SetupPluginModeLogic(CurrentPluginMode.Value);
        }

        private void SetupPluginModeLogic(PluginMode pluginMode)
        {
            switch (pluginMode)
            {
                case PluginMode.RelationshipBased:
                    _logic = new RelationshipBased();
                    break;

                case PluginMode.Advanced:
                    _logic = new Advanced();
                    break;

                default:
                    _logic = new Disabled();
                    break;
            }
        }

        private void CurrentPluginMode_SettingChanged(object sender, EventArgs e)
        {
            SetupPluginModeLogic(sender is ConfigEntry<PluginMode> configMode ? configMode.Value : PluginMode.Disabled);
        }

        public void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            Logger.LogError($"Not configuring {nameof(GameDialogHelper)} because it's broken");
            return;

            CharacterApi.RegisterExtraBehaviour<GameDialogHelperController>(GUID);

            Harmony.CreateAndPatchAll(typeof(Hooks));
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (_supportedSceneNames.Contains(arg0.name))
            {
                CurrentlyEnabled = true;
                _targetHeroine = null;
            }
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (_supportedSceneNames.Contains(arg0.name))
            {
                CurrentlyEnabled = false;
                _targetHeroine = null;
            }
        }


        internal static bool EnabledForCurrentHeroine()
        {
            var result = CurrentlyEnabled && _logic.EnabledForHeroine(TargetHeroine);
            Logger.LogDebug($"EnabledForCurrentHeroine => {result}");
            return result;
        }

        internal static bool EnabledForCurrentQuestion()
        {
            var result = CurrentlyEnabled && CurrentDialog != null && _logic.EnabledForQuestion(TargetHeroine, CurrentDialog);
            Logger.LogDebug($"EnabledForCurrentQuestion => {result}");
            return result;
        }

        internal static bool EnabledForCurrentQuestionAnswer(int answer)
        {
            var result =  CurrentlyEnabled && CurrentDialog != null &&
                   _logic.EnableForAnswer(TargetHeroine, CurrentDialog, answer);
            Logger.LogDebug($"EnabledForCurrentQuestionAnswer({answer}) => {result}");
            return result;
        }

        /*
        internal static void HighlightSelection(int idx, ref string[] args, bool correct = true)
        {
            string[] tmp = args[idx].Split(splitter, 2, StringSplitOptions.None);
            if (tmp.Length == 2)
            {
                if (AutoTranslator.TryTranslate(tmp[0], out string translatedText))
                {
                    tmp[0] = translatedText;
                }
                args[idx] = string.Join(string.Empty, new string[] { tmp[0], correct ? Logic.CorrectHighlight : Logic.IncorrectHighlight, splitter[0], tmp[1] });
            }
        }
        */

        internal static void HighlightSelections(ref string[] args)
        {
            Logger.LogDebug($"HighlightSelections for {CurrentDialog.QuestionInfo}");
            if (EnabledForCurrentQuestion())
            {
                // skip first entry in array
                for (var i = 1; i < args.Length; i++)
                {
                    var answerId = i - 1;
                    if (!EnabledForCurrentQuestionAnswer(answerId)) continue;

                    var suffix = answerId == CurrentDialog.CorrectAnswerId
                        ? _logic.CorrectHighlight
                        : _logic.IncorrectHighlight;
                    if (string.IsNullOrEmpty(suffix)) continue;

                    var tmp = args[i].Split(Splitter, 2, StringSplitOptions.None);
                    if (tmp.Length != 2) continue;
                    Logger.LogDebug($"HighlightSelections: {i}: {args[i]}");

                    if (AutoTranslator.TryTranslate(tmp[0], out var translatedText))
                    {
                        tmp[0] = translatedText;
                    }

                    args[i] = string.Join(string.Empty, new[] {tmp[0], suffix, Splitter[0], tmp[1]});
                }
            }
        }

        internal static void ProcessDialogAnswered() => _logic.ProcessDialogAnswered(TargetHeroine, CurrentDialog);
        

        internal delegate int InfoCheckSelectConditionsDelegate(Info obj, int conditions);
    }
}
