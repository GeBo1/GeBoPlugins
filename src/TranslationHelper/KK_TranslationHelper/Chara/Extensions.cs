using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using UnityEngine;

namespace TranslationHelperPlugin.Chara
{
    public static partial class Extensions
    {
        public static IEnumerator TranslateFullNameCoroutine(this ChaFile chaFile, Action<string> callback)
        {
            int started;

            var nameTypes = new[] { "lastname", "firstname" };

            var names = nameTypes.Select(n => string.Empty).ToList();   

            started = names.Count;
            for(var i = 0; i < nameTypes.Length; i++)
            {
                var dest = i;
                var nameTypeIndex = GeBoAPI.Instance.ChaFileNameToIndex(nameTypes[i]);
                var name = names[i] = chaFile.GetName(nameTypeIndex);

                chaFile.StartMonitoredCoroutine(
                    CardNameTranslationManager.Instance.TranslateCardName(name, 
                        new NameScope(chaFile.GetSex(), chaFile.GetNameType(nameTypeIndex)), r =>
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

            callback(string.Join(TranslationHelper.SpaceJoiner, names.ToArray()));
        }
    }
}
