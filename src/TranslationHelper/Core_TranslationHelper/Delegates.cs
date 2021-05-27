using System;
using GeBoCommon.AutoTranslation;
using JetBrains.Annotations;
using Studio;
#if AI||HS2

#endif

namespace TranslationHelperPlugin
{
    public delegate void TranslationResultHandler(ITranslationResult translationResult);

    // Slow implementations should return false immediately when fast == true
    public delegate bool TryAlternateStudioCharaLoaderTranslation(NameScope sexOnlyScope, CharaFileInfo charaFileInfo, string originalName, bool fast);

    [PublicAPI]
    public delegate NameScope NameScopeGetter();
}

