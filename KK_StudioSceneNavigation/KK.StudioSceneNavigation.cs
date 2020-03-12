using Illusion.Game;

namespace StudioSceneNavigationPlugin
{
    public partial class StudioSceneNavigation
    {
        private const int IMAGES_PER_PAGE = 12;

        private void PlayNotificationSound(NotificationSound notificationSound)
        {
            if (NotificationSoundsEnabled.Value)
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
}