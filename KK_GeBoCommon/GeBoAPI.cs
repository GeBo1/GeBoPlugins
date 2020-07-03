using Illusion.Game;
using System;
using System.Collections.Generic;
using GeBoCommon.Chara;

namespace GeBoCommon
{
    public partial class GeBoAPI
    {

        private static readonly IList<KeyValuePair<string, NameType>> ChaFileNamesInternal =
            new List<KeyValuePair<string, NameType>>
            {
                new KeyValuePair<string, NameType>("firstname", NameType.Given),
                new KeyValuePair<string, NameType>("lastname", NameType.Family),
                new KeyValuePair<string, NameType>("nickname", NameType.Unclassified)
            }.AsReadOnly();

        public IEnumerable<KeyValuePair<int, string>> ChaFileEnumerateNames(ChaFile chaFile)
        {
            if (chaFile == null) yield break;
            var i = -1;
            yield return new KeyValuePair<int, string>(++i, chaFile.parameter?.firstname);
            yield return new KeyValuePair<int, string>(++i, chaFile.parameter?.lastname);
            yield return new KeyValuePair<int, string>(++i, chaFile.parameter?.nickname);
        }

        public string ChaFileFullName(ChaFile chaFile) => chaFile?.parameter.fullname;

        public void ChaFileSetName(ChaFile chaFile, int index, string chaName)
        {
            switch (index)
            {
                case 0:
                    chaFile.parameter.firstname = chaName;
                    break;

                case 1:
                    chaFile.parameter.lastname = chaName;
                    break;

                case 2:
                    chaFile.parameter.nickname = chaName;
                    break;

                default:
                    throw new InvalidOperationException($"{index} is not a valid chaName index for this game");
            }
        }

        public void PlayNotification(NotificationSound notificationSound)
        {
            SystemSE sound;
            switch (notificationSound)
            {
                case NotificationSound.Success:
                    sound = SystemSE.result_single;
                    break;

                case NotificationSound.Error:
                    sound = SystemSE.cancel;
                    break;

                default:
                    sound = SystemSE.result_end;
                    break;
            }

            Utils.Sound.Play(sound);
        }
    }
}
