using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
#if AI || HS2
using AIChara;
#endif

namespace GeBoCommon
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Game differences")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Game differences")]
    public interface IGeBoAPI
    {
        IAutoTranslationHelper AutoTranslationHelper { get; }
        IEnumerable<KeyValuePair<int, string>> ChaFileEnumerateNames(ChaFile chaFile);

        string ChaFileFullName(ChaFile chaFile);

        void ChaFileSetName(ChaFile chaFile, int index, string chaName);

        void PlayNotification(NotificationSound notificationSound);

        int ChaFileNameToIndex(string name);
        string ChaFileIndexToName(int index);

        NameType ChaFileIndexToNameType(int index);

        int ChaFileNameCount { get; }

        IList<string> ChaFileNames { get; }
    }
}
