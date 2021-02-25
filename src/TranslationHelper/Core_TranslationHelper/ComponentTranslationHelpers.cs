using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ADV;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
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
            

            void ResultHandler(ITranslationResult result)
            {
                if (result.Succeeded) textComponent.SafeProc(tc => tc.text = result.TranslatedText);
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
                textComponent.StartCoroutine(TranslateFullNameCoroutine(context.OriginalText, chaControl, scope,
                    ResultHandler));
            }

            return true;
        }

        private static IEnumerator TranslateFullNameCoroutine(string origName, ChaControl chaControl, NameScope scope,
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

            var job = TranslationHelper.CardNameManager.TranslateCardName(origName, scope, false, handler);

            yield return chaControl != null
                ? chaControl.StartMonitoredCoroutine(job)
                : TranslationHelper.Instance.StartCoroutine(job);




        }
    }
}
