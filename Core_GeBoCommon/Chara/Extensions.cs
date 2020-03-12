using System.Collections.Generic;
#if AI
using AIChara;
#endif

namespace GeBoCommon.Chara
{
    public static partial class Extensions
    {
        public static IEnumerable<KeyValuePair<int, string>> IterNames(this ChaFile chaFile)
        {
            return GeBoAPI.Instance.ChaFileIterNames(chaFile);
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