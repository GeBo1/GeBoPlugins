using GeBoCommon.AutoTranslation;
using Studio;

namespace TranslationHelperPlugin
{
    public delegate void TranslationResultHandler(ITranslationResult translationResult);

    public delegate bool TryAlternateStudioCharaLoaderTranslation(CharaFileInfo charaFileInfo, string originalName);

}

