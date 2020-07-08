using System;
using System.Collections;
using System.Collections.Generic;
using GeBoCommon;
using GeBoCommon.Chara;
using UnityEngine;

namespace TranslationHelperPlugin.Chara
{
    public static partial class Extensions
    {
        public static IEnumerator TranslateFullNameCoroutine(this ChaFile chaFile, Action<string> callback)
        {
            int started;

            var names = new List<string>
            {
                chaFile.GetName(GeBoAPI.Instance.ChaFileNameToIndex("lastname")),
                chaFile.GetName(GeBoAPI.Instance.ChaFileNameToIndex("firstname"))
            };

            started = names.Count;
            for (var i = 0; i < names.Count; i++)
            {
                var dest = i;

                chaFile.StartMonitoredCoroutine(
                    CardNameTranslationManager.Instance.TranslateCardName(names[i],
                        new NameScope(chaFile.GetSex(), chaFile.GetNameType(i)), r =>
                        {
                            if (r.Succeeded)
                            {
                                names[dest] = r.TranslatedText;
                            }

                            started--;
                        }));
            }

            yield return new WaitUntil(() => started == 0);

            if (TranslationHelper.KK_GivenNameFirst.Value)
            {
                names.Reverse();
            }

            callback(string.Join(" ", names.ToArray()));
        }
    }
}
