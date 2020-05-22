using System.Collections.Generic;
#if AI
using AIChara;
#endif

namespace GeBoCommon.Chara
{
    public static class Extensions
    {
        public static IEnumerable<KeyValuePair<int, string>> EnumerateNames(this ChaFile chaFile)
        {
            return GeBoAPI.Instance.ChaFileEnumerateNames(chaFile);
        }

        public static void SetName(this ChaFile chaFile, int index, string name)
        {
            GeBoAPI.Instance.ChaFileSetName(chaFile, index, name);
        }

        public static string GetFullName(this ChaFile chaFile)
        {
            return GeBoAPI.Instance.ChaFileFullName(chaFile);
        }
    }
}
