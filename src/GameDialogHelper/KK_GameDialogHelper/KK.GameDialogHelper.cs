using System;
using System.Collections.Generic;
using ActionGame.Communication;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GameDialogHelperPlugin.PluginModeLogic;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.MainGame;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

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
        public const string Version = "0.9.9";

        private const float ColorDelta = 2f / 3f;

        internal static new ManualLogSource Logger;
        private static GameDialogHelper _instance;

        public static GameDialogHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameDialogHelper>();
                }

                return _instance;
            }
        }


        private static readonly SimpleLazy<InfoCheckSelectConditionsDelegate> InfoCheckSelectConditionsLoader =
            new SimpleLazy<InfoCheckSelectConditionsDelegate>(() =>
            {
                var csc = AccessTools.Method(typeof(Info), "CheckSelectConditions");
                return (InfoCheckSelectConditionsDelegate)Delegate.CreateDelegate(
                    typeof(InfoCheckSelectConditionsDelegate), csc);
            });

        private static readonly string[] Splitter = {",tag"};
        private static SaveData.Heroine _targetHeroine;

        private static IPluginModeLogic _logic;

        private static readonly Dictionary<int, HighlightType> CurrentDialogHighlights =
            new Dictionary<int, HighlightType>();

        internal static Color DefaultColor = new Color(1f, 1f, 1f);
        internal static Color DefaultCorrectColor = new Color(ColorDelta, 1f, ColorDelta);
        internal static Color DefaultIncorrectColor = new Color(1f, ColorDelta, ColorDelta);

        private readonly HashSet<string> _supportedSceneNames = new HashSet<string>(new[] {"Talk"});

        internal readonly HashSet<SaveData.Heroine> LoadedFromCard = new HashSet<SaveData.Heroine>();


        internal static DialogInfo CurrentDialog { get; private set; }
        public static ConfigEntry<PluginMode> CurrentPluginMode { get; private set; }
        public static ConfigEntry<RelationshipLevel> MinimumRelationshipLevel { get; private set; }
        public static ConfigEntry<string> CorrectHighlight { get; private set; }
        public static ConfigEntry<string> IncorrectHighlight { get; private set; }
        public static ConfigEntry<HighlightMode> HighlightMode { get; private set; }
        public static bool CurrentlyEnabled { get; private set; }
        private static IAutoTranslationHelper AutoTranslator => GeBoAPI.Instance.AutoTranslationHelper;
        private Guid _currentPlayerGuid = Guid.Empty;
        public Guid CurrentSessionGuid { get; internal set; } = Guid.Empty;
        public Guid CurrentSaveGuid { get; internal set; } = Guid.Empty;

        public Guid CurrentPlayerGuid
        {
            get
            {
                if (_currentPlayerGuid == Guid.Empty && Game.IsInstance())
                {
                    Game.Instance.SafeProc(g => g.Player.SafeProc(p => _currentPlayerGuid = p.GetCharaGuid()));
                }

                return _currentPlayerGuid;
            }
        }

        public static SaveData.Heroine TargetHeroine
        {
            get
            {
                if (!CurrentlyEnabled) return null;
                if (_targetHeroine == null)
                {
                    FindObjectOfType<TalkScene>().SafeProcObject(
                        ts => _targetHeroine = ts.targetHeroine);
                }

                return _targetHeroine;
            }
        }

        internal static InfoCheckSelectConditionsDelegate InfoCheckSelectConditions =>
            InfoCheckSelectConditionsLoader.Value;

        internal static void SetCurrentDialog(int questionId, int correctAnswerId, int numChoices)
        {
            CurrentDialog = new DialogInfo(questionId, correctAnswerId, numChoices);
            CurrentDialogHighlights.Clear();
        }

        internal static void ClearCurrentDialog()
        {
            CurrentDialog = null;
            CurrentDialogHighlights.Clear();
        }

        public void Main()
        {
            Logger = base.Logger;

            CurrentPluginMode = Config.Bind("Config", "Plugin Mode", PluginMode.RelationshipBased,
                "Controls how plugin operates");

            HighlightMode = Config.Bind("General", "Highlight Mode", GameDialogHelperPlugin.HighlightMode.ChangeColor,
                "How to signify if an answer is right or wrong");

            CorrectHighlight = Config.Bind("General", "Highlight Character (correct)", "←",
                "String to append to highlighting correct answers");
            IncorrectHighlight = Config.Bind("General", "Highlight Character (incorrect)", "Х",
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
            Logger = base.Logger;

            CharacterApi.RegisterExtraBehaviour<GameDialogHelperCharaController>(GUID);
            CharacterApi.GetRegisteredBehaviour(typeof(GameDialogHelperCharaController)).MaintainState = true;

            GameAPI.RegisterExtraBehaviour<GameDialogHelperGameController>(GUID);

            Harmony.CreateAndPatchAll(typeof(Hooks));
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg1 == LoadSceneMode.Single && arg0.name == "Title")
            {
                DoReset();
                return;
            }

            if (!_supportedSceneNames.Contains(arg0.name)) return;
            CurrentlyEnabled = true;
            _targetHeroine = null;
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (!_supportedSceneNames.Contains(arg0.name)) return;
            _targetHeroine.SafeProc(h => h.PersistData());
            CurrentlyEnabled = false;
            _targetHeroine = null;
        }

        internal static void DoReset()
        {
            GameDialogHelperGameController.DoReset();
            GameDialogHelperCharaController.DoReset();

            Instance.OnReset();
        }

        private void OnReset()
        {
            LoadedFromCard.Clear();
            CurrentSessionGuid = Guid.NewGuid();
            CurrentSaveGuid = Guid.Empty;
            _currentPlayerGuid = Guid.Empty;
        }


        internal static bool EnabledForCurrentHeroine()
        {
            var result = CurrentlyEnabled && _logic.EnabledForHeroine(TargetHeroine);
            Logger?.DebugLogDebug($"EnabledForCurrentHeroine => {result}");
            return result;
        }

        internal static bool EnabledForCurrentQuestion()
        {
            var result = CurrentlyEnabled && CurrentDialog != null &&
                         _logic.EnabledForQuestion(TargetHeroine, CurrentDialog);
            Logger?.DebugLogDebug($"EnabledForCurrentQuestion => {result}");
            return result;
        }

        internal static bool EnabledForCurrentQuestionAnswer(int answer)
        {
            var result = CurrentlyEnabled && CurrentDialog != null &&
                         _logic.EnableForAnswer(TargetHeroine, CurrentDialog, answer);
            Logger?.DebugLogDebug($"EnabledForCurrentQuestionAnswer({answer}) => {result}");
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

        internal static void SaveHighlightSelections(string[] args)
        {
            CurrentDialogHighlights.Clear();
            if (!CurrentlyEnabled || !EnabledForCurrentQuestion()) return;
            // skip first entry in array
            for (var i = 1; i < args.Length; i++)
            {
                var answerId = i - 1;
                if (!EnabledForCurrentQuestionAnswer(answerId)) continue;


                var tmp = args[i].Split(Splitter, 2, StringSplitOptions.None);
                if (tmp.Length != 2) continue;

                var value = answerId == CurrentDialog.CorrectAnswerId
                    ? HighlightType.Correct
                    : HighlightType.Incorrect;

                Logger?.DebugLogDebug($"SaveHighlightSelections: {answerId} {value}");
                CurrentDialogHighlights[answerId] = value;
            }
        }


        internal static void ApplyHighlightSelections(int answerId, TextMeshProUGUI text)
        {
            text.color = DefaultColor;
            if (!CurrentlyEnabled) return;
            if (!CurrentDialogHighlights.TryGetValue(answerId, out _)) return;

            _logic.ApplyHighlightSelection(answerId, text);
        }

#if DEADCODE
        internal static void HighlightSelections(ref string[] args)
        {
            Logger?.DebugLogDebug($"HighlightSelections for {CurrentDialog.QuestionInfo}");
            if (!EnabledForCurrentQuestion()) return;
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
                Logger?.DebugLogDebug($"HighlightSelections: {i}: {args[i]}");

                if (AutoTranslator.TryTranslate(tmp[0], out var translatedText))
                {
                    tmp[0] = translatedText;
                }

                args[i] = string.Join(string.Empty, new[] {tmp[0], suffix, Splitter[0], tmp[1]});
            }
        }
#endif

        internal static void ProcessDialogAnswered(bool isCorrect = false)
        {
            _logic.ProcessDialogAnswered(TargetHeroine, CurrentDialog, isCorrect);
        }

        public static void DefaultApplyHighlightSelection(int answerId, TextMeshProUGUI text)
        {
            if (!CurrentlyEnabled) return;
            var isCorrect = answerId == CurrentDialog.CorrectAnswerId;
            switch (HighlightMode.Value)
            {
                case GameDialogHelperPlugin.HighlightMode.AppendText:
                    var suffix = isCorrect
                        ? _logic.CorrectHighlight
                        : _logic.IncorrectHighlight;
                    if (string.IsNullOrEmpty(suffix)) break;
                    var answer = text.text;
                    if (AutoTranslator.TryTranslate(answer, out var translatedText))
                    {
                        answer = translatedText;
                    }

                    text.text = StringUtils.JoinStrings(" ", answer, suffix);
                    break;

                case GameDialogHelperPlugin.HighlightMode.ChangeColor:

                    text.color = isCorrect ? DefaultCorrectColor : DefaultIncorrectColor;
                    break;
            }
        }


        internal delegate int InfoCheckSelectConditionsDelegate(Info obj, int conditions);
    }
}
