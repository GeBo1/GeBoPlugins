using System.Linq;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using TranslationHelperPlugin.Utils;
using IllusionStudio = Studio.Studio;
#if AI || HS2

#endif

namespace TranslationHelperPlugin.Studio
{
    internal partial class Hooks
    {
        // ReSharper disable InconsistentNaming
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        private static readonly Limiter TreeNodeLimiter = new Limiter(30, nameof(TreeNodeLimiter));

        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(TreeNodeCtrl), "RefreshVisibleLoop")]
        internal static void RefreshVisibleLoopPatch(TreeNodeObject _source)
        {
            if (TranslationHelper.Instance == null || !IllusionStudio.IsInstance() ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled ||
                !Singleton<IllusionStudio>.Instance.dicInfo.TryGetValue(_source, out var ctrlInfo) ||
                !(ctrlInfo is OCIChar oChar))
            {
                return;
            }

            var chaFile = oChar.charInfo?.chaFile;
            if (chaFile == null) return;

            Configuration.UpdateTreeForChar(chaFile);

            var name = chaFile.GetFullName();
            if (string.IsNullOrEmpty(name)) return;
            _source.textName = name;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void CharaList_InitCharaList_Postfix(CharaList __instance)
        {
            TranslateDisplayList(__instance);
        }


        private static bool TryApplyAlternateTranslation(CharaFileInfo charaFileInfo, string origName)
        {
            return Configuration.AlternateStudioCharaLoaderTranslators.Any(tryTranslate =>
                tryTranslate(charaFileInfo, origName));
        }

        private static void TranslateDisplayList(CharaList charaList)
        {
            if (charaList == null || TranslationHelper.Instance == null ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled)
            {
                return;
            }

            var cfiList = Traverse.Create(charaList)?.Field<CharaFileSort>("charaFileSort")?.Value?.cfiList;
            if (cfiList == null) return;

            var sex = Traverse.Create(charaList)?.Field<int>("sex")?.Value ?? -1;
            if (sex == -1) return;

            void HandleResult(CharaFileInfo charaFileInfo, ITranslationResult result)
            {
                if (!result.Succeeded) return;
                charaFileInfo.name = charaFileInfo.node.text = result.TranslatedText;
            }

            var scope = new NameScope((CharacterSex)sex);

            foreach (var entry in cfiList)
            {
                var origName = entry.name;
                if (TryApplyAlternateTranslation(entry, origName))
                {
                    TreeNodeLimiter.EndImmediately();
                    return;
                }

                void Handler(ITranslationResult result)
                {
                    HandleResult(entry, result);
                    if (TryApplyAlternateTranslation(entry, origName))
                    {
                        TreeNodeLimiter.EndImmediately();
                    }

                    TreeNodeLimiter.EndImmediately();
                }

                TranslationHelper.Instance.StartCoroutine(TreeNodeLimiter.Start().AppendCo(
                    TranslationHelper.CardNameManager.TranslateCardName(origName, scope,
                        CardNameTranslationManager.CanForceSplitNameString(origName), Handler)));
            }
        }
    }
}
