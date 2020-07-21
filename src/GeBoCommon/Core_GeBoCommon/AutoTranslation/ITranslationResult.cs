namespace GeBoCommon.AutoTranslation
{
    public interface ITranslationResult
    {
        bool Succeeded { get; }
        string TranslatedText { get; }
        string ErrorMessage { get; }
    }
}