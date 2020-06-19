using System;
using System.Collections.Generic;
using AIChara;
using GeBoCommon.Chara;
using Illusion.Game;
using IllusionStudio = Studio;

namespace GeBoCommon
{
    public partial class GeBoAPI
    {
        private static readonly IList<KeyValuePair<string, NameType>> ChaFileNamesInternal =
            new List<KeyValuePair<string, NameType>>
            {
                new KeyValuePair<string, NameType>("fullname", NameType.Unclassified)
            }.AsReadOnly();


        public IEnumerable<KeyValuePair<int, string>> ChaFileEnumerateNames(ChaFile chaFile)
        {
            yield return new KeyValuePair<int, string>(0, chaFile?.parameter?.fullname);
        }

        public string ChaFileFullName(ChaFile chaFile)
        {
            return chaFile?.parameter.fullname;
        }

        public void ChaFileSetName(ChaFile chaFile, int index, string chaName)
        {
            if (index == 0)
            {
                chaFile.parameter.fullname = chaName;
            }
            else
            {
                throw new InvalidOperationException($"{index} is not a valid name index for this game");
            }
        }

        public void PlayNotification(NotificationSound notificationSound)
        {
            SystemSE sound;
            switch (notificationSound)
            {
                case NotificationSound.Success:
                    sound = SystemSE.ok_l;
                    break;

                case NotificationSound.Error:
                    sound = SystemSE.cancel;
                    break;

                default:
                    sound = SystemSE.ok_s;
                    break;
            }

            Utils.Sound.Play(sound);
        }
    }
}
