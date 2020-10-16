using System.Diagnostics.CodeAnalysis;

namespace GeBoCommon.AutoTranslation
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface ITranslationResult
    {
        bool Succeeded { get; }
        string TranslatedText { get; }
        string ErrorMessage { get; }
    }
}
