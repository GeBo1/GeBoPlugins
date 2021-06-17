using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using static GeBoCommon.Utilities.Delegates;

namespace GeBoCommon.AutoTranslation.Implementation
{
    internal class XUnityAutoTranslationHelper : AutoTranslationHelperBase, IAutoTranslationHelper
    {
        private readonly SimpleLazy<AddTranslationToCacheDelegate> _addTranslationToCache;
        private readonly SimpleLazy<object> _defaultCache;
        private readonly Func<string> _getAutoTranslationsFilePath;
        private readonly Func<HashSet<string>> _getRegisteredRegexes;
        private readonly Func<HashSet<string>> _getRegisteredSplitterRegexes;
        private readonly Func<Dictionary<string, string>> _getReplacements;
        private readonly Func<Dictionary<string, string>> _getTranslations;
        private readonly SimpleLazy<ReloadTranslationsDelegate> _reloadTranslations;

        public XUnityAutoTranslationHelper()
        {
            _defaultCache = new SimpleLazy<object>(LazyReflectionGetter<object>(() => DefaultTranslator, "TextCache"));
            _reloadTranslations = new SimpleLazy<ReloadTranslationsDelegate>(ReloadTranslationsDelegateLoader);
            _addTranslationToCache = new SimpleLazy<AddTranslationToCacheDelegate>(AddTranslationToCacheLoader);

            var settingsType = new SimpleLazy<Type>(() =>
                typeof(IPluginEnvironment).Assembly.GetType("XUnity.AutoTranslator.Plugin.Core.Configuration.Settings",
                    true));

            _getReplacements = LazyReflectionGetter<Dictionary<string, string>>(settingsType, "Replacements");
            _getAutoTranslationsFilePath = LazyReflectionGetter<string>(settingsType, "AutoTranslationsFilePath");
            _getTranslations = LazyReflectionGetter<Dictionary<string, string>>(_defaultCache, "_translations");
            _getRegisteredRegexes = LazyReflectionGetter<HashSet<string>>(_defaultCache, "_registeredRegexes");
            _getRegisteredSplitterRegexes =
                LazyReflectionGetter<HashSet<string>>(_defaultCache, "_registeredSplitterRegexes");
            XUnityAutoTranslationHelperHooks.Setup();
        }


        private static ITranslator DefaultTranslator => AutoTranslator.Default;
        public object DefaultCache => _defaultCache.Value;

        public bool TryTranslate(string untranslatedText, out string translatedText)
        {
            return DefaultTranslator.TryTranslate(untranslatedText, out translatedText);
        }

        public bool TryTranslate(string untranslatedText, int scope, out string translatedText)
        {
            return DefaultTranslator.TryTranslate(untranslatedText, scope, out translatedText);
        }

        public void TranslateAsync(string untranslatedText, Action<ITranslationResult> onCompleted)
        {
            DefaultTranslator.TranslateAsync(untranslatedText, result => onCompleted(new TranslationResult(result)));
        }

        public void TranslateAsync(string untranslatedText, int scope, Action<ITranslationResult> onCompleted)
        {
            DefaultTranslator.TranslateAsync(untranslatedText, scope,
                result => onCompleted(new TranslationResult(result)));
        }

        public void AddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope)
        {
            _addTranslationToCache.Value(key, value, persistToDisk, translationType, scope);
        }

        public void ReloadTranslations()
        {
            _reloadTranslations.Value();
        }

        public bool IsTranslatable(string text)
        {
            return LanguageHelper.IsTranslatable(text);
        }

        ManualLogSource IAutoTranslationHelper.Logger => Common.CurrentLogger;

        public Dictionary<string, string> GetReplacements()
        {
            return _getReplacements();
        }

        public string GetAutoTranslationsFilePath()
        {
            return _getAutoTranslationsFilePath();
        }

        public Dictionary<string, string> GetTranslations()
        {
            return _getTranslations();
        }

        public HashSet<string> GetRegisteredRegexes()
        {
            return _getRegisteredRegexes();
        }

        public HashSet<string> GetRegisteredSplitterRegexes()
        {
            return _getRegisteredSplitterRegexes();
        }


        public void IgnoreTextComponent(object textComponent)
        {
            DefaultTranslator.IgnoreTextComponent(textComponent);
        }

        public void UnignoreTextComponent(object textComponent)
        {
            DefaultTranslator.UnignoreTextComponent(textComponent);
        }

        public void RegisterOnTranslatingCallback(Action<IComponentTranslationContext> context)
        {
            DefaultTranslator.RegisterOnTranslatingCallback(context.AsXUnityContextAction());
        }

        public void UnregisterOnTranslatingCallback(Action<IComponentTranslationContext> context)
        {
            DefaultTranslator.UnregisterOnTranslatingCallback(context.AsXUnityContextAction());
        }

        public int GetCurrentTranslationScope()
        {
            return FallbackGetCurrentTranslationScope();
        }

        public bool IsRedirected(string text)
        {
            return text.IsRedirected();
        }

        public string FixRedirected(string text)
        {
            return text.FixRedirected();
        }

        public string MakeRedirected(string text)
        {
            return text.MakeRedirected();
        }

        public bool ContainsVariableSymbol(string text)
        {
            return LanguageHelper.ContainsVariableSymbols(text);
        }


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
                    Logger.LogDebug(
                        $"Mono bug preventing delegate creation for {method.FullDescription()} ({e.Message}), using workaround instead");

                    Expression<AddTranslationToCacheDelegate> workaround =
                        (key, value, persistToDisk, translationType, scope) =>
                            method.Invoke(DefaultCache,
                                new object[] {key, value, persistToDisk, translationType, scope});

                    addTranslationToCache = workaround.Compile();
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
            return (ReloadTranslationsDelegate)Delegate.CreateDelegate(typeof(ReloadTranslationsDelegate),
                DefaultTranslator, method);
        }

        public class TranslationResult : ITranslationResult
        {
            protected readonly XUnity.AutoTranslator.Plugin.Core.TranslationResult Source;

            internal TranslationResult(XUnity.AutoTranslator.Plugin.Core.TranslationResult src)
            {
                Source = src;
                Succeeded = Source?.Succeeded ?? false;
                TranslatedText = Source?.TranslatedText;
                ErrorMessage = Source?.ErrorMessage;
            }

            public bool Succeeded { get; }
            public string TranslatedText { get; }
            public string ErrorMessage { get; }
        }

        public class ComponentTranslationContext : IComponentTranslationContext
        {
            protected readonly XUnity.AutoTranslator.Plugin.Core.ComponentTranslationContext Source;

            public ComponentTranslationContext(XUnity.AutoTranslator.Plugin.Core.ComponentTranslationContext src)
            {
                Source = src;
            }

            public object Component => Source?.Component;

            public string OriginalText => Source?.OriginalText;

            public string OverriddenTranslatedText => Source?.OverriddenTranslatedText;

            public void ResetBehaviour()
            {
                Source?.ResetBehaviour();
            }

            public void OverrideTranslatedText(string translation)
            {
                Source?.OverrideTranslatedText(translation);
            }

            public void IgnoreComponent()
            {
                Source?.IgnoreComponent();
            }
        }
    }

    public static class XUnityAutoTranslationHelperExtensions
    {
        private static readonly Dictionary<Action<IComponentTranslationContext>, Action<ComponentTranslationContext>>
            ContextWrappers =
                new Dictionary<Action<IComponentTranslationContext>, Action<ComponentTranslationContext>>();

        public static Action<ComponentTranslationContext> AsXUnityContextAction(
            this Action<IComponentTranslationContext> context)
        {
            if (ContextWrappers.TryGetValue(context, out var wrappedContext)) return wrappedContext;
            return ContextWrappers[context] =
                innerContext => context(new XUnityAutoTranslationHelper.ComponentTranslationContext(innerContext));
        }
    }

    internal static class XUnityAutoTranslationHelperHooks
    {
        private static bool _hooked;

        internal static void Setup()
        {
            if (_hooked) return;
            Harmony.CreateAndPatchAll(typeof(XUnityAutoTranslationHelperHooks));
            _hooked = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AutoTranslationPlugin), "LoadTranslations")]
        internal static void TranslationsLoadedPostfix()
        {
            try
            {
                GeBoAPI.OnTranslationLoaded(EventArgs.Empty);
            }

            catch (Exception err)
            {
                Common.CurrentLogger?.LogException(err, nameof(TranslationsLoadedPostfix));
            }
        }
    }
}
