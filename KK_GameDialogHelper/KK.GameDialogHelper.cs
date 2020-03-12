using ActionGame.Communication;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.SceneManagement;

namespace GameDialogHelperPlugin
{
    public enum RelationshipLevel : int
    {
        [Description("Always show correct answers")]
        Anyone = -1,

        [Description("Show correct answers for acquaintances (or higher)")]
        Acquaintance = 0,

        [Description("Show correct answers for friends (or higher)")]
        Friend = 1,

        [Description("Show correct answers only if you're dating")]
        Lover = 2,

        [Description("Disable showing correct answers")]
        Disabled = int.MinValue
    }

    [BepInDependency(GeBoAPI.PluginName, GeBoAPI.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(GeBoCommon.Constants.GameProcessName)]
#if KK
    [BepInProcess(GeBoCommon.Constants.AltGameProcessName)]
#endif
    public partial class GameDialogHelper : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.BepInEx.GameDialogHelper";
        public const string PluginName = "Game Dialog Helper";
        public const string Version = "0.9.0";

        private readonly HashSet<string> SupportedSceneNames = new HashSet<string>(new string[] { "Talk" });

        internal delegate int InfoCheckSelectConditionsDelegate(Info obj, int _conditions);

        internal static new ManualLogSource Logger;
        private static int? successIndex = null;
        private static InfoCheckSelectConditionsDelegate _infoCheckSelectConditions = null;
        private static readonly string[] splitter = { ",tag" };
        private static SaveData.Heroine _targetHeroine = null;

        public static ConfigEntry<RelationshipLevel> MinimumRelationshipLevel { get; private set; }
        public static ConfigEntry<string> Highlight { get; private set; }
        public static bool CurrentlyEnabled { get; private set; }

        internal void Main()
        {
            MinimumRelationshipLevel = Config.Bind("Config", "Minimum Relationship", RelationshipLevel.Friend, "Highlight correct choice if relationship with character is the selected level or higher");
            Highlight = Config.Bind("Config", "Highlight", "←", "String to append to correct answers when highlighting");
        }

        internal void Awake()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Hooks));
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (SupportedSceneNames.Contains(arg0.name))
            {
                CurrentlyEnabled = true;
                _targetHeroine = null;
            }
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (SupportedSceneNames.Contains(arg0.name))
            {
                CurrentlyEnabled = false;
                _targetHeroine = null;
            }
        }

        private static IAutoTranslationHelper AutoTranslator => GeBoAPI.Instance.AutoTranslationHelper;

        public static SaveData.Heroine TargetHeroine
        {
            get
            {
                if (CurrentlyEnabled)
                {
                    if (_targetHeroine is null)
                    {
                        _targetHeroine = FindObjectOfType<TalkScene>()?.targetHeroine;
                    }
                    return _targetHeroine;
                }

                return null;
            }
        }

        internal static bool EnabledForCurrentHeroine()
        {
            return TargetHeroine != null && TargetHeroine.relation >= (int)MinimumRelationshipLevel.Value;
        }

        internal static InfoCheckSelectConditionsDelegate InfoCheckSelectConditions
        {
            get
            {
                if (_infoCheckSelectConditions is null)
                {
                    var csc = AccessTools.Method(typeof(Info), "CheckSelectConditions");
                    _infoCheckSelectConditions = (InfoCheckSelectConditionsDelegate)Delegate.CreateDelegate(typeof(InfoCheckSelectConditionsDelegate), csc);
                }
                return _infoCheckSelectConditions;
            }
        }

        internal static void HighlightSelection(int idx, ref string[] args)
        {
            string[] tmp = args[idx].Split(splitter, 2, StringSplitOptions.None);
            if (tmp.Length == 2)
            {
                if (AutoTranslator.TryTranslate(tmp[0], out string translatedText))
                {
                    tmp[0] = translatedText;
                }
                args[idx] = string.Join(string.Empty, new string[] { tmp[0], Highlight.Value, splitter[0], tmp[1] });
            }
        }
    }
}
