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
            var origName = chaFile.GetFullName();
            chaFile.StartMonitoredCoroutine(
                CardNameTranslationManager.Instance.TranslateCardName(origName, new NameScope(chaFile.GetSex()),
                    Handlers.AddNameToCache(origName),
                    r => { callback(r.Succeeded ? r.TranslatedText : string.Empty); }));
            yield break;
        }
    }
}
