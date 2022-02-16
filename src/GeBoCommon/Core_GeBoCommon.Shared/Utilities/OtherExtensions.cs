using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using JetBrains.Annotations;
using UnityEngine;
using BepInLogLevel = BepInEx.Logging.LogLevel;
using UnityEngineObject = UnityEngine.Object;

namespace GeBoCommon.Utilities
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class OtherExtensions
    {
        private static ManualLogSource Logger => Common.CurrentLogger;

        [PublicAPI]
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> array)
        {
            return array.Select((item, index) => new KeyValuePair<int, T>(index, item));
        }

        [PublicAPI]
        [Conditional("DEBUG")]
        public static void DebugLogDebug(this ManualLogSource logger, object obj)
        {
            logger.LogDebug(obj);
        }

        [PublicAPI]
        public static void LogErrorMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Error, obj);
        }

        [PublicAPI]
        public static void LogFatalMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Fatal, obj);
        }

        [PublicAPI]
        public static void LogInfoMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Info, obj);
        }

        [PublicAPI]
        public static void LogWarningMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Warning, obj);
        }

        [PublicAPI]
        public static void LogException(this ManualLogSource logger, Exception exception, string message = null)
        {
            LogException(logger, exception, null, message);
        }

        [PublicAPI]
        public static void LogException(this ManualLogSource logger, Exception exception, UnityEngineObject context,
            string message = null)
        {
            var msgBuilder = Common.RequestStringBuilder();
            try
            {
                var currentException = exception;
                var level = 0;

                void StartLine()
                {
                    for (var i = 0; i < level; i++) msgBuilder.Append("> ");
                }


                while (currentException != null)
                {
                    StartLine();
                    msgBuilder.Append(currentException.GetPrettyTypeFullName()).Append(": ");

                    if (!string.IsNullOrEmpty(message))
                    {
                        msgBuilder.Append(message).Append(": ");
                    }

                    msgBuilder.Append(currentException.Message);

                    if (level == 0) logger.LogWarning(msgBuilder.ToString());

                    msgBuilder.AppendLine(":");

                    if (level == 0 && context)
                    {
                        StartLine();
                        msgBuilder.Append(" Context: ").Append(context).Append(" (")
                            .Append(context.GetPrettyTypeFullName()).AppendLine(")");
                    }

                    var stackTrace = new StackTrace(currentException, true);
                    var frames = stackTrace.GetFrames();
                    if (frames != null)
                    {
                        foreach (var frame in frames)
                        {
                            StartLine();
                            msgBuilder.Append(" at ").Append(frame.GetMethod());

                            var filename = frame.GetFileName();
                            if (!string.IsNullOrEmpty(filename))
                            {
                                msgBuilder.Append(" in ").Append(frame.GetFileName());
                                var line = frame.GetFileLineNumber();
                                if (line > 1) msgBuilder.Append(": line").Append(line);
                            }

                            msgBuilder.AppendLine();
                        }
                    }

                    level++;
                    currentException = currentException.InnerException;
                    if (currentException == null) break;
                    StartLine();
                    msgBuilder.AppendLine("Inner exception:");
                }

                Logger.LogDebug(msgBuilder.ToString());
            }
            finally
            {
                Common.ReleaseStringBuilder(msgBuilder);
            }
        }

        [PublicAPI]
        public static ulong Sum(this IEnumerable<ulong> source)
        {
            return source.Aggregate((a, c) => a + c);
        }

        [PublicAPI]
        public static string PrettyTypeName(this Type type)
        {
            var typeName = type.Name;
            if (type.GetGenericArguments().Length == 0) return typeName;
            var args = type.GetGenericArguments();
            typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.InvariantCulture));
            return StringUtils.JoinStrings("", typeName, "<",
                StringUtils.JoinStrings(",", args.Select(PrettyTypeName).ToArray()), ">");
        }

        [PublicAPI]
        public static string PrettyTypeFullName(this Type type)
        {
            var typeName = type.FullName ?? type.Name;
            if (type.GetGenericArguments().Length == 0) return typeName;
            var args = type.GetGenericArguments();
            typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.InvariantCulture));
            return StringUtils.JoinStrings("", type.Namespace, ".", typeName, "<",
                StringUtils.JoinStrings(",", args.Select(PrettyTypeFullName).ToArray()), ">");
        }

        [PublicAPI]
        public static string GetPrettyTypeName(this object obj)
        {
            return obj.GetType().PrettyTypeName();
        }

        [PublicAPI]
        public static string GetPrettyTypeFullName(this object obj)
        {
            return obj.GetType().PrettyTypeFullName();
        }


        private static void SafeInvoke<TEventArgs>(Delegate[] eventHandlers, object sender, TEventArgs args)
            where TEventArgs : EventArgs
        {
            foreach (var handler in eventHandlers)
            {
                try
                {
                    //Logger.DebugLogDebug($"{nameof(SafeInvoke)}: {handler.Method.FullDescription()}");
                    handler.DynamicInvoke(sender, args);
                }
                catch (Exception err)
                {
                    if (sender is UnityEngineObject unityObj)
                    {
                        Logger?.LogException(err, unityObj,
                            $"{nameof(SafeInvoke)}<{typeof(TEventArgs).GetPrettyTypeName()}>");
                    }
                    else
                    {
                        Logger?.LogException(err, $"{nameof(SafeInvoke)}<{typeof(TEventArgs).GetPrettyTypeName()}>");
                    }
                }
            }
        }

        [PublicAPI]
        public static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender,
            TEventArgs args) where TEventArgs : EventArgs
        {
            if (eventHandler == null) return;
            SafeInvoke(eventHandler.GetInvocationList(), sender, args);
        }

        [PublicAPI]
        public static void SafeInvoke(this EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler == null) return;
            SafeInvoke(eventHandler.GetInvocationList(), sender, args);
        }

        [PublicAPI]
        public static T GetOrAddComponent<T>(this Component component, Action<T> initializer)
            where T : Component
        {
            return component == null ? null : component.gameObject.GetOrAddComponent(initializer);
        }

        [PublicAPI]
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> initializer)
            where T : Component
        {
            if (gameObject == null) return null;
            if (gameObject.TryGetComponent<T>(out var component)) return component;

            component = gameObject.AddComponent<T>();
            initializer?.Invoke(component);
            return component;
        }

#if !KKS
        [PublicAPI]
        public static bool TryGetComponent<T>(this Component component, out T result) where T : Component
        {
            if (component != null) return component.gameObject.TryGetComponent(out result);
            result = null;
            return false;
        }

        [PublicAPI]
        public static bool TryGetComponent<T>(this GameObject gameObject, out T result) where T : Component
        {
            if (gameObject == null)
            {
                result = null;
                return false;
            }

            result = gameObject.GetComponent<T>();
            return result != null;
        }
#endif
    }
}
