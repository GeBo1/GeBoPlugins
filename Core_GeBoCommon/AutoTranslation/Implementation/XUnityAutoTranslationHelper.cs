using BepInEx.Logging;
using GeBoCommon.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using static GeBoCommon.Utilities.Delegates;

namespace GeBoCommon.AutoTranslation.Implementation
{
    internal class XUnityAutoTranslationHelper : AutoTranslationHelperBase, IAutoTranslationHelper
    {
        private readonly SimpleLazy<AddTranslationToCacheDelegate> _addTranslationToCache;
        private readonly SimpleLazy<ReloadTranslationsDelegate> _reloadTranslations;
        private readonly SimpleLazy<object> _defaultCache;
        private readonly Func<Dictionary<string, string>> _getReplacements;
        private readonly Func<Dictionary<string, string>> _getTranslations;
        private readonly Func<string> _getAutoTranslationsFilePath;
        private readonly Func<HashSet<string>> _getRegisteredRegexes;
        private readonly Func<HashSet<string>> _getRegisteredSplitterRegexes;

        public XUnityAutoTranslationHelper()
        {
            _defaultCache = new SimpleLazy<object>(LazyReflectionGetter<object>(() => DefaultTranslator, "TextCache"));
            _reloadTranslations = new SimpleLazy<ReloadTranslationsDelegate>(ReloadTranslationsDelegateLoader);
            _addTranslationToCache = new SimpleLazy<AddTranslationToCacheDelegate>(AddTranslationToCacheLoader);

            var settingsType = new SimpleLazy<Type>(() => typeof(IPluginEnvironment).Assembly.GetType("XUnity.AutoTranslator.Plugin.Core.Configuration.Settings", true));

            _getReplacements = LazyReflectionGetter<Dictionary<string, string>>(settingsType, "Replacements");
            _getAutoTranslationsFilePath = LazyReflectionGetter<string>(settingsType, "AutoTranslationsFilePath");
            _getTranslations = LazyReflectionGetter<Dictionary<string, string>>(_defaultCache, "_translations");
            _getRegisteredRegexes = LazyReflectionGetter<HashSet<string>>(_defaultCache, "_registeredRegexes");
            _getRegisteredSplitterRegexes = LazyReflectionGetter<HashSet<string>>(_defaultCache, "_registeredSplitterRegexes");
        }

        //private Type SettingsType => _settingsType.Value;
        private ITranslator DefaultTranslator => AutoTranslator.Default;
        public object DefaultCache => _defaultCache.Value;

        private AddTranslationToCacheDelegate AddTranslationToCacheLoader()
        {
            AddTranslationToCacheDelegate addTranslationToCache;

            if (DefaultCache != null)
            {
                var method = AccessTools.Method(DefaultCache.GetType(), "AddTranslationToCache");
                try
                {
                    addTranslationToCache = (AddTranslationToCacheDelegate)Delegate.CreateDelegate(
                        typeof(AddTranslationToCacheDelegate), DefaultCache, method);
                }
                catch (ArgumentException e)
                {
                    Logger.LogWarning($"Mono bug preventing delegate creation for {method.Name}, using workaround: {e.Message}");
                    addTranslationToCache = (key, value, persistToDisk, translationType, scope) => method.Invoke(DefaultCache, new object[] { key, value, persistToDisk, translationType, scope });
                }
            }
            else
            {
                Logger.LogWarning("Unable to expose 'AddTranslationToCache'");
                addTranslationToCache = FallbackAddTranslationToCache;
            }
            return addTranslationToCache;
        }

        private ReloadTranslationsDelegate ReloadTranslationsDelegateLoader()
        {
            var method = AccessTools.Method(DefaultTranslator.GetType(), "ReloadTranslations");
            return (ReloadTranslationsDelegate)Delegate.CreateDelegate(typeof(ReloadTranslationsDelegate), DefaultTranslator, method);
        }

        public bool TryTranslate(string untranslatedText, out string translatedText) => DefaultTranslator.TryTranslate(untranslatedText, out translatedText);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        public void TranslateAsync(string untranslatedText, Action<ITranslationResult> onCompleted)
        {
            DefaultTranslator.TranslateAsync(untranslatedText, (result) => onCompleted(new TranslationResult(result)));
        }

        public void AddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope)
        {
            _addTranslationToCache.Value(key, value, persistToDisk, translationType, scope);
        }

        public void ReloadTranslations() => _reloadTranslations.Value();
        public bool IsTranslatable(string text) => LanguageHelper.IsTranslatable(text);
        ManualLogSource IAutoTranslationHelper.Logger => Logger;

        /*
        private Func<object> DefaultCacheGetterLoader()
        {
            Func<object> _defaultCacheGetter = null;

            var textCacheFieldInfo = AccessTools.Field(DefaultTranslator.GetType(), "TextCache");
            if (textCacheFieldInfo != null)
            {
                _defaultCacheGetter = () => textCacheFieldInfo.GetValue(DefaultTranslator);
            }
            else
            {
                Logger.LogError("Unable to access DefaultCache");
                _defaultCacheGetter = () => null;
            }

            return _defaultCacheGetter;
        }
        */

        public Dictionary<string, string> GetReplacements() => _getReplacements();
        public string GetAutoTranslationsFilePath() => _getAutoTranslationsFilePath();

        public Dictionary<string, string> GetTranslations() => _getTranslations();
        public HashSet<string> GetRegisteredRegexes() => _getRegisteredRegexes();

        public HashSet<string> GetRegisteredSplitterRegexes() => _getRegisteredSplitterRegexes();

        public class TranslationResult : ITranslationResult
        {
            protected readonly XUnity.AutoTranslator.Plugin.Core.TranslationResult Source;
            public bool Succeeded { get; }
            public string TranslatedText { get; }
            public string ErrorMessage { get; }

            internal TranslationResult(XUnity.AutoTranslator.Plugin.Core.TranslationResult src)
            {
                Source = src;
                Succeeded = Source?.Succeeded ?? false;
                TranslatedText = Source?.TranslatedText;
                ErrorMessage = Source?.ErrorMessage;
            }
        }
    }
}
