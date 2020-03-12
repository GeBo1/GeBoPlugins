using GeBoCommon.Utilities;
using System.Globalization;
#if AI
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    public static class TranslationHelperCharaExtensions
    {
        public static string GetRegistrationID(this ChaFile chaFile)
        {
            return chaFile?.GetHashCode().ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}