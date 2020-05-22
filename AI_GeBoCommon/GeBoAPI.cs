using System;
using System.Collections.Generic;
using AIChara;
using AIProject;
using IllusionStudio = Studio;

namespace GeBoCommon
{
    public partial class GeBoAPI
    {
        public IEnumerable<KeyValuePair<int, string>> ChaFileEnumerateNames(ChaFile chaFile)
        {
            yield return new KeyValuePair<int, string>(0, chaFile?.parameter?.fullname);
        }

        public string ChaFileFullName(ChaFile chaFile) => chaFile?.parameter.fullname;

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
            SoundPack.SystemSE sound;
            switch (notificationSound)
            {
                case NotificationSound.Success:
                    sound = SoundPack.SystemSE.OK_L;
                    break;

                case NotificationSound.Error:
                    sound = SoundPack.SystemSE.Error;
                    break;

                default:
                    sound = SoundPack.SystemSE.Cancel;
                    break;
            }

            Singleton<Manager.Resources>.Instance.SoundPack.Play(sound);
        }
    }
}
