using System;

namespace GeBoCommon.AutoTranslation
{
    public class SimpleTranslationResult : ITranslationResult
    {
        public SimpleTranslationResult(bool succeeded, string translatedText = null, string errorMessage = null)
        {
            if (succeeded && translatedText == null)
            {
                throw new ArgumentException("can not be null if succeeded is true", nameof(translatedText));
            }


            Succeeded = succeeded;
            TranslatedText = translatedText;
            ErrorMessage = errorMessage;
        }

        public bool Succeeded { get; }
        public string TranslatedText { get; }
        public string ErrorMessage { get; }
    }
}
