using System;
using System.Diagnostics.CodeAnalysis;
using AIProject;
using AIProject.Scene;
using AIProject.UI;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using HarmonyLib;
using UnityEngine.UI;

namespace TranslationHelperPlugin.Acceleration
{
    internal static partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConfirmScene), nameof(ConfirmScene.Sentence),
            MethodType.Setter)]
        private static void ConfirmSceneSentenceSetterPrefix(ConfirmScene __instance,
            ref string value)
        {
            if (!Configuration.AccelerationEnabled) return;
            var orig = value;

            try
            {
                if (Configuration.ConfirmSceneSentenceHandled.Contains(orig)) return;

                if (Configuration.ConfirmSceneSentenceTranslations.TryGetValue(value, out var translatedText))
                {
                    value = translatedText;
                    return;
                }

                if (GeBoAPI.Instance.AutoTranslationHelper.TryTranslate(value, out translatedText))
                {
                    Configuration.ConfirmSceneSentenceTranslations[orig] = value = translatedText;
                    Configuration.ConfirmSceneSentenceHandled.Add(translatedText);
                    return;
                }


                void ConfirmSceneSentenceTranslationCompleted(ITranslationResult r)
                {
                    if (!r.Succeeded) return;
                    Configuration.ConfirmSceneSentenceTranslations[orig] = r.TranslatedText;
                    Configuration.ConfirmSceneSentenceHandled.Add(r.TranslatedText);
                }

                GeBoAPI.Instance.AutoTranslationHelper.TranslateAsync(orig, ConfirmSceneSentenceTranslationCompleted);
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(ConfirmSceneSentenceSetterPrefix));
            }
#pragma warning restore CA1031
            finally
            {
                Logger.DebugLogDebug($"{nameof(ConfirmSceneSentenceSetterPrefix)}: {orig} => {value}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ConfirmScene), nameof(ConfirmScene.Sentence),
            MethodType.Getter)]
        private static void ConfirmSceneSentenceGetterPostfix(ConfirmScene __instance,
            ref string __result)
        {
            if (!Configuration.AccelerationEnabled) return;
            // ReSharper disable once RedundantAssignment - used in debug builds
            var orig = __result;
            try
            {
                if (Configuration.ConfirmSceneSentenceTranslations.TryGetValue(__result, out var translatedText))
                {
                    __result = translatedText;
                }
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(ConfirmSceneSentenceGetterPostfix));
            }
#pragma warning restore CA1031
            finally
            {
                Logger.DebugLogDebug($"{nameof(ConfirmSceneSentenceGetterPostfix)}: {orig} => {__result}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommandLabel.CommandInfo), nameof(CommandLabel.CommandInfo.OnText),
            MethodType.Setter)]
        private static void CommandLabelCommandInfoOnTextSetterPrefix(Func<string> value)
        {
            // necessary for things like "{0}にアクション" to translate without being mangled
            if (!Configuration.AccelerationEnabled) return;
            try
            {
                StringMethodTranspilerHelper.PatchMethod(
                    new Harmony($"{nameof(CommandLabelCommandInfoOnTextSetterPrefix)}_{value.Method.Name}_Patcher"),
                    value.Method);
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(CommandLabelCommandInfoOnTextSetterPrefix));
            }
#pragma warning restore CA1031
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(HomeMenu), nameof(HomeMenu.OnBeforeStart))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony Patch")]
        private static void HomeMenuOnBeforeStartPrefix(Text ____temperatureLabel, Text ____timeLabel)
        {
            try
            {
                if (!Configuration.AccelerationEnabled) return;
                ____temperatureLabel.SafeProc(GeBoAPI.Instance.AutoTranslationHelper.IgnoreTextComponent);
                ____timeLabel.SafeProc(GeBoAPI.Instance.AutoTranslationHelper.IgnoreTextComponent);
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(HomeMenuOnBeforeStartPrefix));
            }
#pragma warning restore CA1031
        }
    }
}
