using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using KKAPI.Chara;
using UnityEngine;
#if AI || HS2
using AIChara;

#endif


namespace TranslationHelperPlugin.Chara
{
    public static partial class Extensions
    {
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

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Game differences")]
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
            TranslationHelper.Logger?.DebugLogDebug($"Extensions.SetTranslatedName: {chaFile} {index} {name}");
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

        public static void OnTranslationComplete(this ChaFile chaFile)
        {
            chaFile.SafeProc(cf => cf.GetTranslationHelperController().SafeProcObject(c => c.OnTranslationComplete()));
        }

        public static void TranslateFullName(this ChaFile chaFile, Action<string> callback)
        {
            chaFile.StartMonitoredCoroutine(chaFile.TranslateFullNameCoroutine(callback));
        }
    }
}
