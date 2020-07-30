using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GeBoCommon
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Game differences")]
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
#endif
        [Obsolete("Use SupportedRegexCompilationOption instead")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Backwards Compatibility")]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Backwards Compatibility")]
        public const RegexOptions SupportedRegexComplitationOption = SupportedRegexCompilationOption;

        [Obsolete("Use MainGameProcessName instead")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Backwards Compatibility")]
        public const string GameProcessName = MainGameProcessName;

#if (KK||AI)
        [Obsolete("Use MainGameProcessNameSteam instead")]
        public const string AltGameProcessName = MainGameProcessNameSteam;
#endif

    }
}
