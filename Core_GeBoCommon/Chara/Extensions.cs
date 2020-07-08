using System.Collections.Generic;
using System.Linq;
#if AI || HS2
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

        public static string GetName(this ChaFile chaFile, string nameType)
        {
            return chaFile.GetName(GeBoAPI.Instance.ChaFileNameToIndex(nameType));
        }

        public static string GetName(this ChaFile chaFile, int index)
        {
            return chaFile.EnumerateNames().Where(e => e.Key == index).Select(e => e.Value).FirstOrDefault();
        }

        public static NameType GetNameType(this ChaFile chaFile, int index)
        {
            var _ = chaFile;
            return GeBoAPI.Instance.ChaFileIndexToNameType(index);
        }

        public static string GetFullName(this ChaFile chaFile)
        {
            return GeBoAPI.Instance.ChaFileFullName(chaFile);
        }


        public static CharacterSex GetSex(this ChaFile chaFile)
        {
            try
            {
                return (CharacterSex)chaFile.parameter.sex;
            }
            catch
            {
                return CharacterSex.Unspecified;
            }
        }
    }
}
