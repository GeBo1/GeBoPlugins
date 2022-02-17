using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ADV;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using TranslationHelperPlugin.Chara;
using UnityEngine.UI;
#if AI||HS2
using AIChara;

#endif

namespace TranslationHelperPlugin
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class ComponentTranslationHelpers
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        [PublicAPI]
        public static ChaControl GetCurrentCharacter(ADVScene advScene)
        {
            ChaControl result = null;
            advScene.SafeProc(advS => advS.Scenario.SafeProc(s => s.currentChara.SafeProc(cd => result = cd.chaCtrl)));
            return result;
        }

        public static bool TryTranslateFullName(IComponentTranslationContext context, Predicate<Text> predicate,
            Func<ChaControl> charGetter, CharacterSex sex = CharacterSex.Unspecified)
        {
            if (!(context.Component is Text textComponent)) return false;
            if (!predicate(textComponent)) return false;

            var originalText = context.OriginalText;

            void ResultHandler(ITranslationResult result)
            {
                // this fires with a simple full-name translation, which is less likely to be accurate than
                // some other translation options in games with split first/last names, so if it's already changed
                // we discard it here.
                if (result.Succeeded)
                {
                    textComponent.SafeProc(tc =>
                    {
                        if (tc.text == result.TranslatedText) return;
                        if (tc.text != originalText)
                        {
                            Logger?.DebugLogDebug(
                                $"text already updated from '{originalText}' to '{tc.text}', discarding '{result.TranslatedText}'");
                            return;
                        }

                        tc.text = result.TranslatedText;
                    });
                }
            }

            var chaControl = charGetter();
            var scope = chaControl != null ? new NameScope((CharacterSex)chaControl.sex) :
                sex != CharacterSex.Unspecified ? new NameScope(sex) : NameScope.DefaultNameScope;

            if (TranslationHelper.TryFastTranslateFullName(scope, context.OriginalText, out var translatedName))
            {
                context.OverrideTranslatedText(translatedName);
            }
            else
            {
                Logger?.DebugLogDebug(
                    $"{nameof(ComponentTranslationHelpers)}.{nameof(TryTranslateFullName)}: attempting async translation for {context.OriginalText} ({chaControl})");
                textComponent.StartCoroutine(TranslateFullNameCoroutine(context.OriginalText, chaControl, scope,
                    textComponent,
                    ResultHandler));
            }

            return true;
        }

        private static IEnumerator TranslateFullNameCoroutine(string origName, ChaControl chaControl, NameScope scope,
            Text textComponent,
            TranslationResultHandler handler)
        {
            // wait a frame to avoid slowing down XUA TranslatingCallback handling
            yield return null;
            if (!TranslationHelper.NameNeedsTranslation(origName, scope))
            {
                yield break;
            }

            if (chaControl != null && chaControl.chaFile != null)
            {
                if (TranslationHelper.CardNameManager.CardNeedsTranslation(chaControl.chaFile) &&
                    chaControl.TryGetTranslationHelperController(out var translationController))
                {
                    translationController.TranslateCardNames();
                    yield return translationController.WaitOnTranslations();
                }
            }
            else
            {
                yield return null; // give more accurate translation a chance to finish
                if (textComponent == null) yield break;
                if (textComponent.text != origName &&
                    !TranslationHelper.NameNeedsTranslation(textComponent.text, scope))
                {
                    Logger?.DebugLogDebug(
                        $"{nameof(ComponentTranslationHelpers)}.{nameof(TranslateFullNameCoroutine)}: translated elsewhere, aborting async translation");
                    yield break;
                }
            }

            var job = TranslationHelper.CardNameManager.TranslateCardName(origName, scope, false, handler);

            yield return chaControl != null
                ? chaControl.StartMonitoredCoroutine(job)
                : TranslationHelper.Instance.StartCoroutine(job);
        }
    }
}
