using System;
using System.Diagnostics.CodeAnalysis;
using ActionGame;
using BepInEx.Logging;
using ChaCustom;
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

        // used in maker, starting new game, editing roster
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.AddList))]
        [HarmonyPatch(typeof(ClassRoomFileListCtrl), nameof(ClassRoomFileListCtrl.AddList))]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "HarmonyPatch")]
        internal static void FileListCtrlAddListPrefix(CustomFileListCtrl __instance, int index, ref string name,
            string club, string personality, string fullpath)
        {
            try
            {
                if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
                var wrapper = new DummyFileInfoWrapper(index, name, club, personality, fullpath);
                Translation.Hooks.FileListCtrlAddListPrefix(__instance, wrapper);
                name = wrapper.Name;
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(FileListCtrlAddListPrefix));
            }
#pragma warning restore CA1031
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerEnter))]
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
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerExit))]
        public static void OnPointerExitPrefix()
        {
            Translation.Hooks.OnPointerExitPrefix();
        }

        // Free H version
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClassRoomFileListCtrl), nameof(ClassRoomFileListCtrl.Create))]
        public static void ClassRoomFileListCtrlCreatePostfix(ClassRoomFileListCtrl __instance)
        {
            void OnEnter(CustomFileInfo info)
            {
                var wrapper = CharaFileInfoWrapper.CreateWrapper(info);
                Translation.Hooks.OnPointerEnterPostfix(__instance, wrapper);
            }

            void OnExit(CustomFileInfo info)
            {
                Translation.Hooks.OnPointerExitPrefix();
            }

            __instance.OnPointerEnter += OnEnter;
            __instance.OnPointerExit += OnExit;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.ChangeItem))]
        public static void ChangeItemPostfix(CustomFileListCtrl __instance, GameObject obj)
        {
            try
            {
                if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
                if (obj == null) return;
                var component = obj.GetComponent<CustomFileInfoComponent>();
                if (component == null) return;

                var name = component.info.name;

                var selectDrawName = Traverse.Create(__instance)?.Field("selectDrawName");
                if (Configuration.ListInfoNameTranslatedMap.TryGetValue(component.info.FullPath, out var tmpName))
                {
                    component.info.name = tmpName;
                    if (selectDrawName != null && selectDrawName.FieldExists()) selectDrawName.SetValue(tmpName);
                    return;
                }


                var sex = Configuration.GuessSex(component.info.club, component.info.personality);

                void Handler(ITranslationResult result)
                {
                    var newName = result.TranslatedText;
                    component.info.name = Configuration.ListInfoNameTranslatedMap[component.info.FullPath] = newName;
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
    }
}
