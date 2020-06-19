using System;
using System.Collections;
using AIChara;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Chara
{
    public static partial class Extensions
    {
        public static IEnumerator TranslateFullNameCoroutine(this ChaFile chaFile, Action<string> callback)
        {
            chaFile.StartMonitoredCoroutine(
                CardNameTranslationManager.Instance.TranslateCardName(chaFile.GetFullName(), new NameScope(chaFile.GetSex()),
                    r => { callback(r.Succeeded ? r.TranslatedText : string.Empty); }));
            yield break;
        }
    }
}
