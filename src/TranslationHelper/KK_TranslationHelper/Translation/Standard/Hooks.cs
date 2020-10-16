using System.Diagnostics.CodeAnalysis;
using ActionGame;
using ChaCustom;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using HarmonyLib;
using UnityEngine;

namespace TranslationHelperPlugin.Translation.Standard
{
    internal class Hooks
    {
        internal static void Setup()
        {
            Utils.CharaFileInfoWrapper.RegisterWrapperType(typeof(CustomFileInfo), typeof(Utils.CustomFileInfoWrapper));
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        // ReSharper disable once IdentifierTypo
        // used in maker, starting new game, editing roster
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.AddList))]
        [HarmonyPatch(typeof(ClassRoomFileListCtrl), nameof(ClassRoomFileListCtrl.AddList))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void FileListCtrlAddListPrefix(CustomFileListCtrl __instance, int index, ref string name,
            string club, string personality, string fullpath)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            var wrapper = new Utils.DummyFileInfoWrapper(index, name, club, personality, fullpath);
            Translation.Hooks.FileListCtrlAddListPrefix(__instance, wrapper);
            name = wrapper.Name;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerEnter))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        public static void OnPointerEnterPostfix(CustomFileListCtrl __instance, GameObject obj)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;

            if (obj == null) return;
            var component = obj.GetComponent<CustomFileInfoComponent>();
            if (component == null) return;

            var wrapper = Utils.CharaFileInfoWrapper.CreateWrapper(component.info);
            

            Translation.Hooks.OnPointerEnterPostfix(__instance, wrapper);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerExit))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        public static void OnPointerExitPrefix() => Translation.Hooks.OnPointerExitPrefix();

        // Free H version
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClassRoomFileListCtrl), nameof(ClassRoomFileListCtrl.Create))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        public static void ClassRoomFileListCtrlCreatePostfix(ClassRoomFileListCtrl __instance)
        {
            void OnEnter(CustomFileInfo info)
            {
                var wrapper = Utils.CharaFileInfoWrapper.CreateWrapper(info);
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
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        public static void ChangeItemPostfix(CustomFileListCtrl __instance, GameObject obj)
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
                TranslationHelper.CardNameManager.TranslateFullName(name, new NameScope((CharacterSex)sex), Handler));
        }

        
    }
}
