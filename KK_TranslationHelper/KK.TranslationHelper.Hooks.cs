using System.Collections.Generic;
using System.Linq;
using ChaCustom;
using GeBoCommon.AutoTranslation;
using HarmonyLib;

namespace TranslationHelperPlugin
{
    partial class TranslationHelper
    {
        internal partial class Hooks
        {
            // maker lists
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.AddList))]
            private static void CustomFileListCtrl_AddList(CustomFileListCtrl __instance, int index, ref string name, string club, string personality)
            {
                if (Instance == null || string.IsNullOrEmpty(club) ||
                    Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly) return;

                byte sex = (byte)(club == "帯刀" && string.IsNullOrEmpty(personality) ? 0 : 1);

                var origName = name;
                void Handler(ITranslationResult result)
                {
                    if (!result.Succeeded) return;
                    var lstFileInfo = Traverse.Create(__instance)?.Field<List<CustomFileInfo>>("lstFileInfo")?.Value;
                    var entry = lstFileInfo?.FirstOrDefault(x => x.index == index && x.name == origName);
                    if (entry == null) return;
                    entry.name = result.TranslatedText;
                }

                Instance.StartCoroutine(CardNameManager.TranslateCardName(name, sex, Handler));
            }
        }
    }
}
