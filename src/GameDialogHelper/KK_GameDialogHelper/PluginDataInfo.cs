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
        public const int MinimumSupportedDataVersion = 4;

        internal static class Keys
        {
            public const string SaveGuid = nameof(SaveGuid);
            public const string DialogMemory = nameof(DialogMemory);
            public const string HeroineGuid = nameof(HeroineGuid);
            public const string HeroineGuidVersion = nameof(HeroineGuidVersion);
        }
    }
}
