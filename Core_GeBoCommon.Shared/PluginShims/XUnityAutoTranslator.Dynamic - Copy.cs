#if !AUTOTRANSLATOR
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GeBoCommon.PluginShims
{
    public partial class XUnityAutoTranslator
    {
        private void Initialize()
        {
            _tryTranslate = NoPluginTryTranslate;
            if (PluginInfo != null)
            {
                var autoTranslator = AccessTools.TypeByName("XUnity.AutoTranslator.Plugin.Core.AutoTranslator");
                if (autoTranslator != null)
                {
                    var defaultTranslatorProp = AccessTools.Property(autoTranslator, "Default");
                    if (defaultTranslatorProp != null)
                    {
                        _defaultTranslator = defaultTranslatorProp.GetValue(null, null);
                    }
                }
            }
            if (DefaultTranslator != null)
            {
                var iTranslatorMethods = AccessTools.GetMethodNames(DefaultTranslator).Where((n) => n.Contains(".ITranslator."));
                var name = iTranslatorMethods.FirstOrDefault((n) => n.EndsWith("TryTranslate"));
                if (!name.IsNullOrEmpty())
                {
                    var tryTranslate = AccessTools.Method(DefaultTranslator.GetType(), name);
                    if (tryTranslate != null)
                    {
                        _tryTranslate = (TryTranslateDelegate)Delegate.CreateDelegate(typeof(TryTranslateDelegate), DefaultTranslator, tryTranslate);
                    }
                }

                name = iTranslatorMethods.FirstOrDefault((n) => n.EndsWith("TranslateAsync"));
                if (!name.IsNullOrEmpty())
                {
                    var translateAsync = AccessTools.Method(DefaultTranslator.GetType(), name);
                    if (translateAsync != null)
                    {
                        var parameters = translateAsync.GetParameters();
                        if (parameters?.Length == 2)
                        {
                            var actionType = parameters[1].ParameterType;
                            if (actionType != null)
                            {
                                var genericArgs = actionType.GetGenericArguments();
                                if (genericArgs.Length > 0)
                                {
                                    var buildCompletionWrapperMethod = AccessTools.Method(this.GetType(), nameof(BuildCompletionWrapper))?.MakeGenericMethod(genericArgs[0]);
                                    if (buildCompletionWrapperMethod != null)
                                    {
                                        _translateAsync = (TranslateAsyncDelegate)((untranslatedText, onCompleted) =>
                                         {
                                             var wrapper = buildCompletionWrapperMethod.Invoke(null, new object[] { onCompleted });
                                             translateAsync.Invoke(DefaultTranslator, new object[] { untranslatedText, wrapper });
                                         });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _translateAsync = _translateAsync ?? (TranslateAsyncDelegate)((_, onCompleted) =>
            {
                onCompleted(new TranslationResultWrapper("Translator Plugin not available"));
            });
        }

        private static Action<T> BuildCompletionWrapper<T>(Action<TranslationResultWrapper> handler)
        {
            return new Action<T>((result) => handler(new TranslationResultWrapper(result)));
        }
        public partial class TranslationResultWrapper
        {
            public bool Succeeded { get; private set; }
            public string TranslatedText { get; private set; }
            public string ErrorMessage { get; private set; }

            private const string UnableToWrap = "Unable to wrap object passed to TranslationResultWrapper";
            private readonly static Dictionary<Type, Dictionary<string, FieldInfo>> getters = new Dictionary<Type, Dictionary<string, FieldInfo>>();
            private void Initialize()
            {
                Succeeded = GetFieldValue("Succeeded", false);
                TranslatedText = GetFieldValue("TranslatedText", string.Empty);
                ErrorMessage = GetFieldValue("ErrorMessage", UnableToWrap);
            }

            internal TranslationResultWrapper(string errorMessage)
            {
                Initialize();
                ErrorMessage = errorMessage;
            }
            private T GetFieldValue<T>(string fieldName, T defaultValue)
            {
                FieldInfo fieldInfo = null;
                if (Source != null)
                {
                    Type type = Source.GetType();
                    if (!getters.TryGetValue(type, out Dictionary<string, FieldInfo> typeGetters))
                    {
                        getters[type] = typeGetters = new Dictionary<string, FieldInfo>();
                    }
                    if (!typeGetters.TryGetValue(fieldName, out fieldInfo))
                    {
                        typeGetters[fieldName] = fieldInfo = AccessTools.Field(type, fieldName);
                    }
                }
                if (fieldInfo is null)
                {
                    return defaultValue;
                }
                return (T)fieldInfo.GetValue(this);
            }
        }
    }
}
#endif