using System;
using System.Collections.Generic;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using KKAPI.MainGame;
using TMPro;
using TranslationHelperPlugin.Chara;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Hooks
    {
        private static IEnumerable<T> FindChildComponents<T>(Component component, string fieldName,
            params string[] paths) where T : Component
        {
            var result = Traverse.Create(component)?.Field<T>(fieldName)?.Value;
            if (result != null) yield return result;

            foreach (var path in paths)
            {
                var transform = component.transform.Find(path);
                if (transform == null) continue;

                result = transform.GetComponent<T>();
                if (result != null) yield return result;
            }
        }

        private static bool ShouldProcess(Component component, ChaFile chaFile)
        {
            return component != null && chaFile != null &&
                   TranslationHelper.Instance.CurrentCardLoadTranslationEnabled &&
                   (!chaFile.TryGetTranslationHelperController(out var controller) ||
                    (!controller.TranslationInProgress && !controller.IsTranslated));
        }

        // Student Cards in roster/new game and Free H selection
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudentCardControlComponent), nameof(StudentCardControlComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        [HarmonyPatch(typeof(CharaHInfoComponent), nameof(CharaHInfoComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        internal static void RosterSetCharaInfoPrefix(Component __instance, ChaFileControl chaFileCtrl,
            ref object __state)
        {
            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;
            try
            {
                __state = false;
                if (!ShouldProcess(__instance, chaFileCtrl) ||
                    !GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(chaFileCtrl.GetFullName()))
                {
                    return;
                }

                __state = true;
                chaFileCtrl.StartMonitoredCoroutine(TranslationHelper.CardNameManager.TranslateCardNames(chaFileCtrl));
            }
            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(RosterSetCharaInfoPrefix));
            }
            finally
            {
                Logger.DebugLogDebug($"RosterSetCharaInfoPrefix: {Time.realtimeSinceStartup - start:000.0000000000}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StudentCardControlComponent), nameof(StudentCardControlComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        [HarmonyPatch(typeof(CharaHInfoComponent), nameof(CharaHInfoComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        internal static void RosterSetCharaInfoPostfix(Component __instance, ChaFileControl chaFileCtrl, object __state)
        {
            try
            {
                if (__state == null || !(bool)__state) return;

                void UpdateDisplayedCard(string name)
                {
                    if (string.IsNullOrEmpty(name)) return;
                    foreach (var textName in FindChildComponents<Text>(__instance, "textName",
                                 "resize/text/name", "resize/image/chara"))
                    {
                        textName.SafeProc(tn => tn.text = name);
                    }
                }

                chaFileCtrl.TranslateFullName(UpdateDisplayedCard);

                FreeHUpdateUI();
            }

            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(RosterSetCharaInfoPostfix));
            }
        }

        private static void FreeHUpdateUI()
        {
            // for 3P the character names show in one extra place that the standard check doesn't cover.
            // easiest to just update each time.
            var freeHScene = Object.FindObjectOfType<FreeHScene>();
            if (freeHScene == null) return;
            var member = Traverse.Create(freeHScene).Field<FreeHScene.Member>("member")?.Value;
            if (member == null) return;

            Action<string> GetUpdateUIField(string fieldName)
            {
                void Callback(string value)
                {
                    if (string.IsNullOrEmpty(value) || freeHScene == null) return;
                    var field = Traverse.Create(freeHScene).Field<TextMeshProUGUI>(fieldName)?.Value;
                    if (field == null) return;
                    field.text = value;
                }

                return Callback;
            }

            Action<string> GetUpdateTextCallback(string path)
            {
                void Callback(string value)
                {
                    if (string.IsNullOrEmpty(value)) return;

                    var obj = GameObject.Find(path);
                    if (obj == null) return;
                    var uiText = obj.GetComponent<Text>();
                    if (uiText == null) return;
                    uiText.text = value;
                }

                return Callback;
            }

            var callbackMap = DictionaryPool<SaveData.Heroine, List<Action<string>>>.Get();
            try
            {
                if (member.resultHeroine.HasValue && member.resultHeroine.Value != null)
                {
                    if (!callbackMap.TryGetValue(member.resultHeroine.Value, out var callbacks))
                    {
                        callbackMap[member.resultHeroine.Value] =
                            callbacks = GeBoCommon.Utilities.ListPool<Action<string>>.Get();
                    }

                    callbacks.Add(GetUpdateUIField("textFemaleName1"));
                }

                if (member.resultPartner.HasValue && member.resultPartner.Value != null)
                {
                    if (!callbackMap.TryGetValue(member.resultPartner.Value, out var callbacks))
                    {
                        callbackMap[member.resultPartner.Value] =
                            callbacks = GeBoCommon.Utilities.ListPool<Action<string>>.Get();
                    }

                    callbacks.Add(GetUpdateUIField("textFemaleName2"));
                }

                // leaving traverse to work with earlier versions
                var resultDarkHeroine = Traverse.Create(member)
                    .Field<ReactiveProperty<SaveData.Heroine>>("resultDarkHeroine")?.Value;
                if (resultDarkHeroine != null && resultDarkHeroine.HasValue && resultDarkHeroine.Value != null)
                {
                    if (!callbackMap.TryGetValue(resultDarkHeroine.Value, out var callbacks))
                    {
                        callbackMap[resultDarkHeroine.Value] =
                            callbacks = GeBoCommon.Utilities.ListPool<Action<string>>.Get();
                    }

                    callbacks.Add(
                        // ReSharper disable once StringLiteralTypo
                        GetUpdateTextCallback("/FreeHScene/Canvas/Panel/Dark/FemaleInfomation/Name/TextMeshPro Text"));
                }

                foreach (var entry in callbackMap)
                {
                    foreach (var heroineChaFile in entry.Key.GetRelatedChaFiles())
                    {
                        heroineChaFile.TranslateFullName(
                            translated =>
                            {
                                foreach (var callback in entry.Value)
                                {
                                    try
                                    {
                                        callback(translated);
                                    }
                                    catch (Exception err)
                                    {
                                        Logger.LogException(err, freeHScene, nameof(FreeHUpdateUI));
                                    }
                                }
                            });
                    }
                }
            }
            finally
            {
                foreach (var entry in callbackMap) GeBoCommon.Utilities.ListPool<Action<string>>.Release(entry.Value);
                DictionaryPool<SaveData.Heroine, List<Action<string>>>.Release(callbackMap);
            }
        }
    }
}
