using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace GeBoCommon
{
    [PublicAPI]
    public static class Constants
    {
#if AI
        public const string GameName = "AI Girl";
        public const string MainGameProcessName = "AI-Syoujyo";
        public const string MainGameProcessNameSteam = "AI-Shoujo";
        public const string StudioProcessName = "StudioNEOV2";
        public const string Prefix = "AI";
        public const RegexOptions SupportedRegexCompilationOption = RegexOptions.Compiled;
#elif EC
        public const string GameName = "Emotion Creators";
        public const string MainGameProcessName = "EmotionCreators";
        public const string StudioProcessName = "***NOPE***";
        public const string Prefix = "EC";
        public const RegexOptions SupportedRegexCompilationOption = RegexOptions.None;
#elif HS
        public const string GameName = "Honey Select";
        public const string MainGameProcessName = "HoneySelect_64";
        public const string BattleArenaProcessName = "BattleArena_64";
        public const string StudioProcessName = "StudioNEO_64";
        public const string Prefix = "HS";
        public const RegexOptions SupportedRegexCompilationOption = RegexOptions.None;
#elif HS2
        public const string GameName = "Honey Select 2";
        public const string MainGameProcessName = "HoneySelect2";
        public const string MainGameProcessNameVR = "HoneySelect2VR";
        public const string StudioProcessName = "StudioNEOV2";
        public const string Prefix = "HS2";
        public const RegexOptions SupportedRegexCompilationOption = RegexOptions.Compiled;
#elif KK
        public const string GameName = "Koikatsu";
        public const string MainGameProcessName = "Koikatu";
        public const string MainGameProcessNameSteam = "Koikatsu Party";
        public const string MainGameProcessNameVR = "KoikatuVR";
        public const string MainGameProcessNameVRSteam = "Koikatsu Party VR";
        public const string StudioProcessName = "CharaStudio";
        public const string Prefix = "KK";
        public const RegexOptions SupportedRegexCompilationOption = RegexOptions.None;
#elif KKS
        public const string GameName = "Koikatsu Sunshine";
        public const string MainGameProcessName = "KoikatsuSunshine";
        public const string TrialProcessName = "KoikatsuSunshineTrial";
        public const string StudioProcessName = "CharaStudio";
        public const string Prefix = "KKS";
        public const RegexOptions SupportedRegexCompilationOption = RegexOptions.Compiled;
#endif
        public const string RepoUrl = "https://github.com/GeBo1/GeBoPlugins";
    }
}
