namespace TranslationHelperPlugin
{
    public class TranslationResult : GeBoCommon.AutoTranslation.SimpleTranslationResult 
    {
        public TranslationResult(bool succeeded, string translatedText = null, string errorMessage = null) :
            base(succeeded, translatedText, errorMessage) { }

        public TranslationResult(string originalText, string translatedText = null, string errorMessage = null) :
            this(!string.IsNullOrEmpty(translatedText) && !TranslationHelper.NameStringComparer.Equals(originalText, translatedText),
                translatedText, errorMessage) { }

    }
}
