﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;
using JetBrains.Annotations;
using BepInLogLevel = BepInEx.Logging.LogLevel;
using Object = UnityEngine.Object;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public static class OtherExtensions
    {
        private static ManualLogSource Logger => Common.CurrentLogger;
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> array)
        {
            return array.Select((item, index) => new KeyValuePair<int, T>(index, item));
        }

        [Conditional("DEBUG")]
        public static void DebugLogDebug(this ManualLogSource logger, object obj)
        {
            logger.LogDebug(obj);
        }

        public static void LogErrorMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Error, obj);
        }

        public static void LogFatalMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Fatal, obj);
        }

        public static void LogInfoMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Info, obj);
        }

        public static void LogWarningMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Warning, obj);
        }

        public static void LogException(this ManualLogSource logger, Exception exception, string message = null)
        {
            LogException(logger, exception, null, message);
        }

        public static void LogException(this ManualLogSource logger, Exception exception, Object context,
            string message = null)
        {
            logger.LogWarning(string.IsNullOrEmpty(message)
                ? $"{exception.GetPrettyTypeFullName()}: {exception.Message}"
                : $"{exception.GetPrettyTypeFullName()}: {message}: {exception.Message}");
            if (context == null)
            {
                UnityEngine.Debug.LogException(exception);
            }
            else
            {
                UnityEngine.Debug.LogException(exception, context);
            }
        }

        public static ulong Sum(this IEnumerable<ulong> source)
        {
            return source.Aggregate((a, c) => a + c);
        }

        public static string PrettyTypeName(this Type type)
        {
            var typeName = type.Name;
            if (type.GetGenericArguments().Length == 0) return typeName;
            var args = type.GetGenericArguments();
            typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.InvariantCulture));
            return StringUtils.JoinStrings("", typeName, "<",
                StringUtils.JoinStrings(",", args.Select(PrettyTypeName).ToArray()), ">");
        }

        public static string PrettyTypeFullName(this Type type)
        {
            var typeName = type.FullName ?? type.Name;
            if (type.GetGenericArguments().Length == 0) return typeName;
            var args = type.GetGenericArguments();
            typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.InvariantCulture));
            return StringUtils.JoinStrings("", type.Namespace, ".", typeName, "<",
                StringUtils.JoinStrings(",", args.Select(PrettyTypeFullName).ToArray()), ">");
        }

        public static string GetPrettyTypeName(this object obj)
        {
            return obj.GetType().PrettyTypeName();
        }

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
                    handler.DynamicInvoke(sender, args);
                }
                catch (Exception err)
                {
                    if (sender is Object unityObj)
                    {
                        Logger?.LogException(err, unityObj, "Unexpected error during event handling");
                    }
                    else
                    {
                        Logger?.LogException(err, "Unexpected error during event handling");
                    }
                }
            }
        }

        public static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender,
            TEventArgs args) where TEventArgs : EventArgs
        {
            if (eventHandler == null) return;
            SafeInvoke(eventHandler.GetInvocationList(), sender, args);
        }

        public static void SafeInvoke(this EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler == null) return;
            SafeInvoke(eventHandler.GetInvocationList(), sender, args);
        }
    }
}
