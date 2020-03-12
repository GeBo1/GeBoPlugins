using BepInEx.Logging;
using System;
using GeBoCommon.Utilities;
namespace GeBoCommon.AutoTranslation
{
    internal class AutoTranslationHelperBase
    {
        private readonly SimpleLazy<ManualLogSource> _logger = null;
        protected ManualLogSource Logger => _logger.Value;

        public AutoTranslationHelperBase()
        {
            _logger = new SimpleLazy<ManualLogSource>(() => BepInEx.Logging.Logger.CreateLogSource(GetType().Name));
        }

        protected bool FallbackTryTranslate(string _, out string translatedText)
        {
            translatedText = string.Empty;
            return false;
        }

        protected void FallbackAddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope)
        {
            _ = (persistToDisk || translationType == scope || (key ?? value).IsNullOrEmpty());
        }

        protected bool FallbackIsTranslatable(string _) => false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        protected void FallbackTranslateAsync(string _, Action<ITranslationResult> onCompleted, ITranslationResult cannedResult)
        {
            onCompleted(cannedResult);
        }
    }
}
