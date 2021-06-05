using System;
using BepInEx.Logging;
using ChaCustom;
using FileListUI;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using TranslationHelperPlugin.Utils;
using UnityEngine;

namespace TranslationHelperPlugin.Translation.Standard
{
    internal class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            CharaFileInfoWrapper.RegisterWrapperType(typeof(CustomFileInfo), typeof(CustomFileInfoWrapper));
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        #if false

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>),
            nameof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>.OnPointerEnter))]
        public static void OnPointerEnterPostfix(CustomFileListCtrl __instance, GameObject obj)
        {
            try
            {
                if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;

                if (obj == null) return;
                var component = obj.GetComponent<CustomFileInfoComponent>();
                if (component == null) return;

                var wrapper = CharaFileInfoWrapper.CreateWrapper(component.info);


                Translation.Hooks.OnPointerEnterPostfix(__instance, wrapper);
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(OnPointerEnterPostfix));
            }
#pragma warning restore CA1031
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>),
            nameof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>.OnPointerExit))]
        public static void OnPointerExitPrefix()
        {
            Translation.Hooks.OnPointerExitPrefix();
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>),
            nameof(ThreadFileListCtrl<CustomFileInfo, CustomFileInfoComponent>.ChangeItem))]
        public static void ChangeItemPostfix(CustomFileListCtrl __instance, CustomFileInfoComponent fic)
        {
            try
            {
                if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
                if (fic == null) return;

                var name = fic.info.name;

                var selectDrawName = Traverse.Create(__instance)?.Field("selectDrawName");
                if (Configuration.ListInfoNameTranslatedMap.TryGetValue(fic.info.FullPath, out var tmpName))
                {
                    fic.info.name = tmpName;
                    if (selectDrawName != null && selectDrawName.FieldExists()) selectDrawName.SetValue(tmpName);
                    return;
                }


                var sex = Configuration.GuessSex(fic.info.club, fic.info.personality);

                void Handler(ITranslationResult result)
                {
                    var newName = result.TranslatedText;
                    fic.info.name = Configuration.ListInfoNameTranslatedMap[fic.info.FullPath] = newName;
                    if (!result.Succeeded) return;
                    if (selectDrawName != null && selectDrawName.FieldExists())
                    {
                        selectDrawName.SetValue(newName);
                    }
                }

                TranslationHelper.Instance.StartCoroutine(
                    TranslationHelper.CardNameManager.TranslateFullName(
                        name, new NameScope((CharacterSex)sex), Handler));
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(ChangeItemPostfix));
            }
#pragma warning restore CA1031
        }

#endif
    }

}
