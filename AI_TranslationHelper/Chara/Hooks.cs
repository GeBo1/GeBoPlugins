namespace TranslationHelperPlugin.Chara
{
    internal partial class Hooks
    {
        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
        internal static void CreateListPrefix() => TranslationHelper.AlternateLoadEventsEnabled = true;
        */

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
        internal static void CreateListPostfix() => TranslationHelper.AlternateLoadEventsEnabled = false;
        */
    }
}
