using System.Diagnostics.CodeAnalysis;

namespace GameDialogHelperPlugin
{
    internal static class PluginDataInfo
    {
        /*
         * DataVersion history
         *
         * 1 - initial nested dictionary version
         * 2 - CharaDialogMemory
         * 3 - reworked AnswerMemory adding Recall
         * 4 - reworked keys names
         */
        public const int DataVersion = 4;
        public const int MinimumSupportedGameDataVersion = 4;
        public const int MinimumSupportedCardDataVersion = 4;

        public const int CurrentSaveGuidVersion = 1;
        public const int MaxSaveGuidVersion = CurrentSaveGuidVersion;

        public const int CurrentCharaGuidVersion = 6;
        public const int MaxCharaGuidVersion = CurrentCharaGuidVersion;
        public const int MinimumSupportedCharaGuidVersion = 5;

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        internal static class Keys
        {
            public const string SaveGuid = nameof(SaveGuid);
            public const string SaveGuidVersion = nameof(SaveGuidVersion);
            public const string DialogMemory = nameof(DialogMemory);
            public const string CharaGuid = nameof(CharaGuid);
            public const string CharaGuidVersion = nameof(CharaGuidVersion);
            public const string PlayerGuid = nameof(PlayerGuid);
            public const string PlayerGuidVersion = nameof(PlayerGuidVersion);
            public const string LastUpdated = nameof(LastUpdated);
        }
    }
}
