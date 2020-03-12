using BepInEx;
using GeBoCommon.Chara;
using Illusion.Game;
using System;
using System.Collections.Generic;

namespace GeBoCommon
{
    public partial class GeBoAPI
    {
        public IEnumerable<KeyValuePair<int, string>> ChaFileIterNames(ChaFile chaFile)
        {
            Logger.LogDebug($"ChaFileIterNames({chaFile}) [{chaFile.parameter}]");
            int i = 0;
            yield return new KeyValuePair<int, string>(i++, chaFile?.parameter?.firstname);
            yield return new KeyValuePair<int, string>(i++, chaFile?.parameter?.lastname);
            yield return new KeyValuePair<int, string>(i++, chaFile?.parameter?.nickname);
        }

        public string ChaFileFullName(ChaFile chaFile) => chaFile?.parameter.fullname;

        public void ChaFileSetName(ChaFile chaFile, int index, string name)
        {
            switch (index)
            {
                case 0:
                    chaFile.parameter.firstname = name;
                    break;

                case 1:
                    chaFile.parameter.lastname = name;
                    break;

                case 2:
                    chaFile.parameter.nickname = name;
                    break;

                default:
                    throw new InvalidOperationException($"{index} is not a valid name index for this game");
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