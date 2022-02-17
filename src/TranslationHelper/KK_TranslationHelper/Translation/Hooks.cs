using System;
using System.Collections.Generic;
using System.Linq;
using ChaCustom;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        private static Coroutine _pointerEnterCoroutine;


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), nameof(CustomCharaFile.Initialize))]
        internal static void CustomCharaFileInitializePrefix()
        {
            Configuration.LoadCharaFileMonitorEnabled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaFile), nameof(CustomCharaFile.Initialize))]
        internal static void CustomCharaFileInitializePostfix()
        {
            Configuration.LoadCharaFileMonitorEnabled = false;
        }

        [UsedImplicitly]
        internal static string ProcessTranslationResult(byte sex, string origName, string fullPath,
            ITranslationResult result)
        {
            return ProcessTranslationResult(new NameScope((CharacterSex)sex), origName, fullPath, result);
        }

        internal static string ProcessTranslationResult(NameScope sexOnlyScope, string origName, string fullPath,
            ITranslationResult result)
        {
            Assert.AreEqual(origName, origName);
            if (result.Succeeded)
            {
                CharaFileInfoTranslationManager.CacheRecentTranslation(sexOnlyScope, fullPath, result.TranslatedText);
            }

            return result.TranslatedText;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        private static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance, string filename,
            bool __result)
        {
            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;
            try
            {
                if (!__result || !Configuration.LoadCharaFileMonitorEnabled || __instance == null ||
                    __instance.parameter.fullname.IsNullOrWhiteSpace() ||
                    !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled)
                {
                    return;
                }

                var origName = __instance.parameter.fullname;
                var scope = new NameScope((CharacterSex)__instance.parameter.sex);

                void Handler(string translatedName)
                {
                    ProcessTranslationResult(scope, origName, filename,
                        new TranslationResult(origName, translatedName));
                }

                if (TranslationHelper.TryFastTranslateFullName(scope, origName, filename, out var fastName))
                {
                    Handler(fastName);
                    return;
                }

                __instance.TranslateFullName(Handler);
            }
            catch (Exception err)
            {
                Logger.LogException(err, nameof(ChaFileControl_LoadCharaFile_Postfix));
            }
            finally
            {
                Logger.DebugLogDebug(
                    $"ChaFileControl_LoadCharaFile_Postfix: {Time.realtimeSinceStartup - start:000.0000000000}");
            }
        }

        internal static void FileListCtrlAddListPrefix(MonoBehaviour fileListCtrl, ICharaFileInfo info)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled || fileListCtrl == null) return;

            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;
            try
            {
                var origName = info.Name;
                var sex = Configuration.GuessSex(info.Club, info.Personality);
                var scope = new NameScope((CharacterSex)sex);
                var path = info.FullPath;

                void Handler(ITranslationResult result)
                {
                    var newName = ProcessTranslationResult(scope, origName, path, result);
                    if (fileListCtrl == null) return;
                    if (TranslationHelper.NameStringComparer.Equals(origName, newName)) return;
                    var lstFileInfo = Traverse.Create(fileListCtrl)?.Field<List<CustomFileInfo>>("lstFileInfo")?.Value;
                    if (lstFileInfo == null) return;


                    Logger.DebugLogDebug($"{nameof(FileListCtrlAddListPrefix)}.{nameof(Handler)}: {lstFileInfo}");

                    var entry = lstFileInfo.FirstOrDefault(x =>
                    {
                        int index;
                        try
                        {
                            index = CharaFileInfoWrapper.CreateWrapper(x).Index;
                        }
                        catch
                        {
                            index = -1;
                        }

                        return info.Index == index;
                    });
                    if (entry == null) return;
                    entry.SafeNameUpdate(path, origName, newName);
                }

                //TranslationHelper.Instance.StartCoroutine(
                fileListCtrl.StartCoroutine(TranslationHelper.TranslateFileInfo(info, Handler));
            }
            catch (Exception err)
            {
                Logger.LogException(err, fileListCtrl, nameof(FileListCtrlAddListPrefix));
            }
            finally
            {
                Logger.DebugLogDebug($"FileListCtrlAddListPrefix: {Time.realtimeSinceStartup - start:000.0000000000}");
            }
        }

        internal static void OnPointerEnterPostfix(MonoBehaviour instance, ICharaFileInfo wrapper)
        {
            try
            {
                if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
                var name = wrapper.Name;
                //Logger.LogDebug($"OnPointerEnterPostfix: name={name}");

                var textDrawName = Traverse.Create(instance)?.Field<Text>("textDrawName")?.Value;

                if (TranslationHelper.TryFastTranslateFullName(wrapper, out var tmpName))
                {
                    wrapper.Name = tmpName;
                    if (textDrawName != null) textDrawName.text = tmpName;
                    return;
                }

                void Handler(ITranslationResult result)
                {
                    //Logger.LogDebug($"OnPointerEnterPostfix: Handler: {result}");
                    /*var newName = ProcessTranslationResult(scope, wrapper.FullPath, name, result);*/
                    if (!result.Succeeded || result.TranslatedText == name) return;
                    wrapper.Name = result.TranslatedText;
                    if (textDrawName == null) return;
                    textDrawName.text = result.TranslatedText;
                }

                if (textDrawName != null) textDrawName.text = name;


                _pointerEnterCoroutine = TranslationHelper.Instance.StartCoroutine(
                    TranslationHelper.TranslateFileInfo(wrapper,
                        //TranslationHelper.CardNameManager.TranslateFullName(name, new NameScope((CharacterSex)sex),
                        Handler, _ => _pointerEnterCoroutine = null));
            }

            catch (Exception err)
            {
                Logger.LogException(err, instance, nameof(OnPointerEnterPostfix));
            }
        }

        internal static void OnPointerExitPrefix()
        {
            try
            {
                if (_pointerEnterCoroutine == null) return;
                TranslationHelper.Instance.StopCoroutine(_pointerEnterCoroutine);
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(OnPointerExitPrefix));
            }

            _pointerEnterCoroutine = null;
        }
    }
}
