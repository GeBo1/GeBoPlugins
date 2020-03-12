using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;

namespace GeBoCommon.PluginShims
{
    public partial class XUnityAutoTranslator : PluginShim<XUnityAutoTranslator>
    {
        private readonly string[] pluginGUIDs = new string[] { "gravydevsupreme.xunity.autotranslator" };
        protected override IEnumerable<string> PluginGUIDs => pluginGUIDs;

        public delegate bool TryTranslateDelegate(string untranslatedText, out string translatedText);
        public delegate void TranslateAsyncDelegate(string untranslatedText, Action<TranslationResultWrapper> onCompleted);

        private object _defaultTranslator = null;
        private TryTranslateDelegate _tryTranslate = null;
        private TranslateAsyncDelegate _translateAsync = null;

        public static ManualLogSource Logger = null;

        public XUnityAutoTranslator()
        {
            Logger = Logger ?? BepInEx.Logging.Logger.CreateLogSource(nameof(XUnityAutoTranslator));
            Initialize();
        }

        private bool NoPluginTryTranslate(string untranslatedText, out string translatedText)
        {
            translatedText = string.Empty;
            return false;
        }

        private object DefaultTranslator => _defaultTranslator;
        public TryTranslateDelegate TryTranslate => _tryTranslate;
        public TranslateAsyncDelegate TranslateAsync => _translateAsync;

        private FieldInfo PluginTextCacheField = null;
        public object DefaultCache
        {
            get
            {
                if (PluginInfo is null || DefaultTranslator is null)
                {
                    return null;
                }

                if (PluginTextCacheField is null)
                {
                    PluginTextCacheField = AccessTools.Field(DefaultTranslator.GetType(), "TextCache");
                }
                return PluginTextCacheField?.GetValue(DefaultTranslator);
            }
        }

        public delegate void AddTranslationToCacheDelegate(string key, string value, bool persistToDisk, int translationType, int scope);

        private AddTranslationToCacheDelegate _addTranslationToCache = null;

        public AddTranslationToCacheDelegate AddTranslationToCache
        {
            get
            {
                if (_addTranslationToCache is null)
                {
                    if (DefaultCache != null)
                    {
                        var method = AccessTools.Method(DefaultCache.GetType(), "AddTranslationToCache");
                        try
                        {
                            _addTranslationToCache = (AddTranslationToCacheDelegate)Delegate.CreateDelegate(
                                typeof(AddTranslationToCacheDelegate), DefaultCache, method);
                        }
                        catch (ArgumentException e)
                        {
                            Logger.LogWarning($"Mono bug preventing delegate creation for {method.Name}, using workaround: {e.Message}");

                            _addTranslationToCache = (key, value, persistToDisk, translationType, scope) =>
                            {
                                method.Invoke(DefaultCache, new object[] { key, value, persistToDisk, translationType, scope });
                            };
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Unable to expose 'AddTranslationToCache'");
                        _addTranslationToCache = (key, value, persistToDisk, translationType, scope) => { };
                    }
                }
                return _addTranslationToCache;
            }
        }
        public partial class TranslationResultWrapper
        {
            public readonly object Source = null;
            internal TranslationResultWrapper(object src)
            {
                Source = src;
                Initialize();
            }
        }
    }
}