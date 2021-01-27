using System;
using GeBoCommon.AutoTranslation;
using Studio;
#if AI||HS2
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    public delegate void TranslationResultHandler(ITranslationResult translationResult);

    public delegate bool TryAlternateStudioCharaLoaderTranslation(NameScope sexOnlyScope, CharaFileInfo charaFileInfo, string originalName);

    public delegate void BehaviorChangedEventHandler(object sender, EventArgs e);

    public delegate NameScope NameScopeGetter();
}

