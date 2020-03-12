using System.Collections.Generic;
#if AI
using AIChara;
#endif

namespace GeBoCommon
{
    public interface IGeBoAPI
    {
        IEnumerable<KeyValuePair<int, string>> ChaFileIterNames(ChaFile chaFile);

        string ChaFileFullName(ChaFile chaFile);

        void ChaFileSetName(ChaFile chaFile, int index, string name);

        void PlayNotification(NotificationSound notificationSound);

        AutoTranslation.IAutoTranslationHelper AutoTranslationHelper { get; }
    }
}