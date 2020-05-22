using System.Collections.Generic;
using GeBoCommon.AutoTranslation;
#if AI
using AIChara;

#endif

namespace GeBoCommon
{
    public interface IGeBoAPI
    {
        IAutoTranslationHelper AutoTranslationHelper { get; }
        IEnumerable<KeyValuePair<int, string>> ChaFileEnumerateNames(ChaFile chaFile);

        string ChaFileFullName(ChaFile chaFile);

        void ChaFileSetName(ChaFile chaFile, int index, string chaName);

        void PlayNotification(NotificationSound notificationSound);
    }
}
