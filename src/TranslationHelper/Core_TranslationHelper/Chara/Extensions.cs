using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI.Chara;
using UnityEngine;
#if AI || HS2
using AIChara;

#endif


namespace TranslationHelperPlugin.Chara
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    [PublicAPI]
    public static partial class Extensions
    {
        private static readonly TranslationTracker TranslateFullNameTracker =
            new TranslationTracker(nameof(TranslateFullNameTracker));

        public static string GetRegistrationID(this ChaFile chaFile)
        {
            string result = null;
            chaFile.SafeProc(a => result = a.GetHashCode().ToString(CultureInfo.InvariantCulture.NumberFormat));
            return result;
        }

        public static Coroutine StartMonitoredCoroutine(this ChaFile chaFile, IEnumerator enumerator)
        {
            ChaControl chaControl = null;
            chaFile.SafeProc(cf => chaControl = cf.GetChaControl());
            return chaControl != null
                ? chaControl.StartMonitoredCoroutine(enumerator)
                : TranslationHelper.Instance.StartCoroutine(enumerator);
        }

        public static Coroutine StartMonitoredCoroutine(this ChaControl chaControl, IEnumerator enumerator)
        {
            Controller controller = null;
            chaControl.SafeProcObject(cc => controller = cc.GetTranslationHelperController());
            return controller != null
                ? controller.StartMonitoredCoroutine(enumerator)
                : TranslationHelper.Instance.StartCoroutine(enumerator);
        }


        public static Controller GetTranslationHelperController(this ChaControl chaControl)
        {
            Controller result = default;
            //TranslationHelper.Logger?.LogDebug($"Extensions.GetTranslationHelperController (chaControl): {chaControl}");

            chaControl.SafeProcObject(cc => cc.gameObject.SafeProcObject(go => result = go.GetComponent<Controller>()));
            return result;
        }

        public static Controller GetTranslationHelperController(this ChaFile chaFile)
        {
            Controller result = default;
            //TranslationHelper.Logger?.LogDebug($"Extensions.GetTranslationHelperController (chaFile): {chaFile}");

            chaFile.SafeProc(
                cf => cf.GetChaControl().SafeProcObject(cc => result = cc.GetTranslationHelperController()));
            return result;
        }

        public static bool TryGetTranslationHelperController(this ChaControl chaControl, out Controller controller)
        {
            controller = GetTranslationHelperController(chaControl);
            return controller != null;
        }

        public static bool TryGetTranslationHelperController(this ChaFile chaFile, out Controller controller)
        {
            controller = GetTranslationHelperController(chaFile);
            return controller != null;
        }


        public static void SetTranslatedName(this ChaFile chaFile, int index, string name)
        {
            //TranslationHelper.Logger?.DebugLogDebug($"Extensions.SetTranslatedName: {chaFile} {index} {name}");
            chaFile.SafeProc(cf =>
                cf.GetTranslationHelperController().SafeProcObject(c => c.SetTranslatedName(index, name)));
        }

        public static string GetOriginalFullName(this ChaFile chaFile)
        {
            return chaFile.TryGetTranslationHelperController(out var controller)
                ? controller.GetOriginalFullName()
#if KK
                : string.Concat(chaFile.GetName("lastname"), " ", chaFile.GetName("firstname"));
#else
                : chaFile.GetFullName();
#endif
        }

        public static string GetFormattedOriginalName(this ChaFile chaFile)
        {
#if KK
            if (!TranslationHelper.ShowGivenNameFirst) return chaFile.GetOriginalFullName();

            return chaFile.TryGetTranslationHelperController(out var controller)
                ? controller.GetFormattedOriginalName()
                : string.Concat(chaFile.GetName("firstname"), " ", chaFile.GetName("lastname"));
#else
            return chaFile.GetOriginalFullName();
#endif
        }

        public static string GetFullPath(this ChaFile chaFile)
        {
            if (!chaFile.TryGetTranslationHelperController(out var controller))
            {
                return Configuration.TryGetCharaFileControlPath(chaFile, out var val1)
                    ? PathUtils.NormalizePath(val1)
                    : null;
            }

            if (!controller.FullPath.IsNullOrEmpty()) return controller.FullPath;

            return Configuration.TryGetCharaFileControlPath(chaFile, out var val2)
                ? PathUtils.NormalizePath(val2)
                : null;
        }

        public static void OnTranslationComplete(this ChaFile chaFile)
        {
            chaFile.SafeProc(cf => cf.GetTranslationHelperController().SafeProcObject(c => c.OnTranslationComplete()));
        }

        public static void TranslateFullName(this ChaFile chaFile, Action<string> callback)
        {
            var origName = chaFile.GetFullName();
            var scope = new NameScope(chaFile.GetSex());


            var wrappedCallback =
                CharaFileInfoTranslationManager.MakeCachingCallbackWrapper(origName, chaFile, scope, callback);
            

            if (TranslationHelper.TryFastTranslateFullName(scope, origName, chaFile.GetFullPath(),
                out string fastName) && !TranslationHelper.NameStringComparer.Equals(origName, fastName))
            {
                wrappedCallback(fastName);
                return;
            }

            chaFile.StartMonitoredCoroutine(chaFile.TranslateFullNameCoroutine(wrappedCallback));
        }

        public static IEnumerator TranslateFullNameCoroutine(this ChaFile chaFile, Action<string> callback)
        {
            var scope = new NameScope(chaFile.GetSex());
            var trackedName = chaFile.GetFullName();

            
            IEnumerator TrackedCoroutine(IEnumerable<TranslationResultHandler> handlers)
            {
                if (TranslationHelper.TryFastTranslateFullName(scope, trackedName, chaFile.GetFullPath(), out var fastName))
                {
                    handlers.CallHandlers(new TranslationResult(trackedName, fastName));
                    yield break;
                }
                
                yield return null;
                
                if (!CardNameTranslationManager.Instance.CardNeedsTranslation(chaFile))
                {
                    // it's possible chaFile updated async
                    var fullName = chaFile.GetFullName();
                    handlers.CallHandlers(new TranslationResult(trackedName, fullName));
                    yield break;
                }


                var names = FullNameNameTypes.Select(n => string.Empty).ToList();


                var started = names.Count;

                void Handler(ITranslationResult result, int i)
                {
                    if (result.Succeeded) names[i] = result.TranslatedText;
                    started--;
                }

                var presetMatched =
                    TranslationHelper.Instance.NamePresetManager.TryTranslateCardNames(chaFile, out var presetMatch);

                IEnumerator TranslateName(int nameIndex)
                {
                    var nameTypeIndex = GeBoAPI.Instance.ChaFileNameToIndex(FullNameNameTypes[nameIndex]);
                    var name = names[nameIndex] = chaFile.GetName(nameTypeIndex);

                    if (presetMatched && presetMatch.TryGetValue(FullNameNameTypes[nameIndex], out var nameMatch) &&
                        !string.IsNullOrEmpty(nameMatch))
                    {
                        Handler(new TranslationResult(true, nameMatch), nameIndex);
                        yield break;
                    }

                    yield return chaFile.StartMonitoredCoroutine(
                        CardNameTranslationManager.Instance.TranslateCardName(name,
                            new NameScope(chaFile.GetSex(), chaFile.GetNameType(nameTypeIndex)),
                            r => Handler(r, nameIndex)));
                }

                for (var i = 0; i < FullNameNameTypes.Length; i++)
                {
                    chaFile.StartMonitoredCoroutine(TranslateName(i));
                }

                bool IsDone()
                {
                    return started < 1;
                }

                yield return new WaitUntil(IsDone);

                if (TranslationHelper.ShowGivenNameFirst)
                {
                    names.Reverse();
                }

                handlers.CallHandlers(new TranslationResult(true,
                    string.Join(TranslationHelper.SpaceJoiner, names.ToArray()).Trim()));
            }


            yield return chaFile.StartMonitoredCoroutine(
                TranslateFullNameTracker.TrackTranslationCoroutine(TrackedCoroutine, scope, trackedName,
                    Translation.Handlers.FileInfoCacheHandler(scope, chaFile.GetFullPath(), trackedName),
                    Translation.Handlers.CallbackWrapper(callback, trackedName)));
        }
    }
}
