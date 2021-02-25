using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if AI || HS2
using AIChara;

#endif

namespace GeBoCommon.Chara
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class Extensions
    {
        /// <summary>
        /// Enumerates the names.
        /// </summary>
        /// <param name="chaFile"/>
        /// <returns>Names as KeyValuePairs (key is index, value is name string)</returns>
        public static IEnumerable<KeyValuePair<int, string>> EnumerateNames(this ChaFile chaFile)
        {
            return GeBoAPI.Instance.ChaFileEnumerateNames(chaFile);
        }

        /// <summary>
        /// Sets name at given index on a ChaFile
        /// </summary>
        /// <param name="chaFile"/>
        /// <param name="index">index of name to set</param>
        /// <param name="name">new name value</param>
        public static void SetName(this ChaFile chaFile, int index, string name)
        {
            GeBoAPI.Instance.ChaFileSetName(chaFile, index, name);
        }

        /// <summary>
        /// Gets the name of given type
        /// </summary>
        /// <param name="chaFile"/>
        /// <param name="nameType">Type of the name.</param>
        /// <returns>name</returns>
        public static string GetName(this ChaFile chaFile, string nameType)
        {
            return chaFile.GetName(GeBoAPI.Instance.ChaFileNameToIndex(nameType));
        }

        /// <summary>
        /// Gets the name at a given index
        /// </summary>
        /// <param name="chaFile" />
        /// <param name="index">The index.</param>
        /// <returns>name</returns>
        public static string GetName(this ChaFile chaFile, int index)
        {
            return chaFile.EnumerateNames().Where(e => e.Key == index).Select(e => e.Value).FirstOrDefault();
        }

        /// <summary>
        /// Gets the type of the name at a given index
        /// </summary>
        /// <param name="chaFile" />
        /// <param name="index">The index.</param>
        /// <returns>name type string</returns>
        public static NameType GetNameType(this ChaFile chaFile, int index)
        {
            var _ = chaFile;
            return GeBoAPI.Instance.ChaFileIndexToNameType(index);
        }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        /// <param name="chaFile"/>
        /// <returns>full name</returns>
        public static string GetFullName(this ChaFile chaFile)
        {
            return GeBoAPI.Instance.ChaFileFullName(chaFile);
        }


        /// <summary>
        /// Gets the sex of the ChaFile
        /// </summary>
        /// <param name="chaFile" />
        /// <returns>sex</returns>
        public static CharacterSex GetSex(this ChaFile chaFile)
        {
            try
            {
                return (CharacterSex)chaFile.parameter.sex;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                return CharacterSex.Unspecified;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
