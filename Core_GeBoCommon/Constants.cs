using System.Text;
using System.Text.RegularExpressions;

namespace GeBoCommon
{
    public static class Constants
    {
#if AI
        public const string GameName = "AI Girl";
        public const string GameProcessName = "AI-Syoujyo";
        public const string StudioProcessName = "StudioNEOV2";
        public const string Prefix = "AI";
        public const RegexOptions SupportedRegexComplitationOption = RegexOptions.Compiled;
#elif EC
        public const string GameName = "Emotion Creators";
        public const string GameProcessName = "EmotionCreators";
        public const string StudioProcessName = "***NOPE***";
        public const string Prefix = "EC";
        public const RegexOptions SupportedRegexComplitationOption = RegexOptions.None;
#elif HS
        public const string GameName = "Honey Select";
        public const string GameProcessNames = "HoneySelect_64";
        public const string BattleArenaProcessName = "BattleArena_64";
        public const string StudioProcessName = "StudioNEO_64";
        public const string Prefix = "HS";
        public const RegexOptions SupportedRegexComplitationOption = RegexOptions.None;
#elif KK
        public const string GameName = "Koikatsu";
        public const string GameProcessName = "Koikatsu";
        public const string AltGameProcessName = "Koikatsu Party";
        public const string StudioProcessName = "CharaStudio";
        public const string Prefix = "KK";
        public const RegexOptions SupportedRegexComplitationOption = RegexOptions.None;
#endif
    }
}
