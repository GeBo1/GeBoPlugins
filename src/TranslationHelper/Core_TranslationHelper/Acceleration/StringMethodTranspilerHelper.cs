using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;

namespace TranslationHelperPlugin.Acceleration
{
    public static class StringMethodTranspilerHelper
    {
        private static Dictionary<MethodInfo, MethodInfo> _replacements;

        private static readonly Stack<int> Scope = new Stack<int>();
        private static readonly HashSet<string> NopeStrings = new HashSet<string>(new[] {"[{0}]"});
        private static ManualLogSource Logger => TranslationHelper.Logger;

        [PublicAPI]
        public static void PatchMethod(Harmony harmony, MethodInfo methodInfo)
        {
            var method = AccessTools.Method(typeof(StringMethodTranspilerHelper),
                nameof(PatchStringFormat));
            if (method == null)
            {
                Logger?.LogError(
                    $"Unable to find {nameof(StringMethodTranspilerHelper)}.{nameof(PatchStringFormat)}");
                return;
            }

            var patch = new HarmonyMethod(method);
            harmony.Patch(methodInfo, transpiler: patch);
            // single calls are often dynamic so don't log by default
            Logger?.DebugLogDebug($"{nameof(StringMethodTranspilerHelper)}: Patched {methodInfo.FullDescription()}");
        }

        [PublicAPI]
        public static void PatchMethod(Harmony harmony, Type type, string methodName)
        {
            var patched = false;
            var method = AccessTools.Method(typeof(StringMethodTranspilerHelper),
                nameof(PatchStringFormat));
            if (method == null)
            {
                Logger?.LogError(
                    $"Unable to find {nameof(StringMethodTranspilerHelper)}.{nameof(PatchStringFormat)}");
                return;
            }

            var patch = new HarmonyMethod(method);
            foreach (var methodInfo in type.GetMethods(AccessTools.allDeclared).Where(m => m.Name == methodName))
            {
                harmony.Patch(methodInfo, transpiler: patch);
                patched = true;
                Logger?.LogDebug($"{nameof(StringMethodTranspilerHelper)}: Patched {methodInfo.FullDescription()}");
            }

            if (patched) return;
            Logger?.LogWarning(
                $"{nameof(StringMethodTranspilerHelper)}: Unable to patch {type.FullName}.{methodName}");
        }

        private static string Format(string format, params object[] args)
        {
            // ReSharper disable once RedundantAssignment - used in debug builds
            var result = string.Empty;
            try
            {
                // ReSharper disable once RedundantAssignment - used in debug builds
                if (!Configuration.AccelerationEnabled) return result = string.Format(format, args);
                Scope.Push(GeBoAPI.Instance.AutoTranslationHelper.GetCurrentTranslationScope());
                try
                {
                    if (NopeStrings.Any(format.Contains)) return string.Format(format, args);

                    object TransArgsSelector(object obj)
                    {
                        return obj is string objStr
                            ? GeBoAPI.Instance.AutoTranslationHelper.FixRedirected(objStr)
                            : obj;
                    }

                    var transFormat = GeBoAPI.Instance.AutoTranslationHelper.FixRedirected(Translate(format));
                    var transArgs = Translate(args).Select(TransArgsSelector).ToArray();


                    result = string.Format(transFormat, transArgs);

                    if (!GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(result))
                    {
                        return GeBoAPI.Instance.AutoTranslationHelper.MakeRedirected(result);
                    }


                    var tmpResult = Translate(result);
                    // strangeness with substitutions
                    if (GeBoAPI.Instance.AutoTranslationHelper.ContainsVariableSymbol(result) ==
                        GeBoAPI.Instance.AutoTranslationHelper.ContainsVariableSymbol(tmpResult))
                    {
                        result = tmpResult;
                    }


                    return GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(result)
                        ? result
                        : GeBoAPI.Instance.AutoTranslationHelper.MakeRedirected(result);
                }
                catch (Exception err)
                {
                    Logger?.LogException(err, "Falling back to standard string.Format");
                    // ReSharper disable once RedundantAssignment - used in debug builds
                    return result = string.Format(format, args);
                }
                finally
                {
                    Scope.Pop();
                }
            }
            finally
            {
                Logger?.DebugLogDebug(
                    $"{nameof(Format)}: {format}, [{string.Join(", ", args.Select(s => s.ToString()).ToArray())}] => {result}");
            }
        }

        private static string Format1(string format, object arg0)
        {
            return Format(format, arg0);
        }

        private static string Format2(string format, object arg0, object arg1)
        {
            return Format(format, arg0, arg1);
        }

        private static string Format3(string format, object arg0, object arg1, object arg2)
        {
            return Format(format, arg0, arg1, arg2);
        }

        private static IEnumerable<CodeInstruction> PatchStringFormat(IEnumerable<CodeInstruction> instructions)
        {
            var replacements = GetMethodReplacements();
            var instructionList = instructions.ToList();
            foreach (var inst in instructionList)
            {
                if (inst.opcode == OpCodes.Call &&
                    replacements.TryGetValue((MethodInfo)inst.operand, out var replacementMethodInfo))
                {
                    inst.operand = replacementMethodInfo;
                }
            }

            return instructionList;
        }

        private static string Translate(string orig)
        {
            // ReSharper disable once RedundantAssignment - used by debug
            var result = orig;
            var scope = Scope.Count > 0 ? Scope.Peek() : -1;
            try
            {
                if (!GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(orig)) return orig;

                result =
                    GeBoAPI.Instance.AutoTranslationHelper.TryTranslate(orig, scope, out var translatedResult)
                        ? translatedResult
                        : orig;
                return result;
#pragma warning disable CA1031
            }
            catch (Exception err)
            {
                Logger?.LogException(err, $"{nameof(Translate)}: Falling back to original string");
                return orig;
            }
#pragma warning restore CA1031

            finally
            {
                Logger?.DebugLogDebug($"{nameof(Translate)}: {orig} => {result} (scope: {scope})");
            }
        }

        private static object Selector(object obj)
        {
            try
            {
                return obj is string strObj ? Translate(strObj) : obj;
            }
            catch
            {
                return obj;
            }
        }

        private static IEnumerable<object> Translate(IEnumerable<object> args)
        {
            return args.Select(Selector);
        }

        private static Dictionary<MethodInfo, MethodInfo> GetMethodReplacements()
        {
            if (_replacements != null) return _replacements;

            _replacements = new Dictionary<MethodInfo, MethodInfo>();
            foreach (var methodInfo in typeof(string).GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (methodInfo.Name != "Format") continue;
                var parameters = methodInfo.GetParameters();
                if (parameters.Length < 2 || parameters.Length > 4 ||
                    parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                var methodName = string.Empty;
                switch (parameters.Length)
                {
                    case 2 when parameters[1].ParameterType.IsArray:
                        methodName = nameof(Format);
                        break;
                    case 2:
                        methodName = nameof(Format1);
                        break;
                    case 3:
                        methodName = nameof(Format2);
                        break;
                    case 4:
                        methodName = nameof(Format3);
                        break;
                }

                if (string.IsNullOrEmpty(methodName)) continue;
                _replacements[methodInfo] = AccessTools.Method(typeof(StringMethodTranspilerHelper), methodName);
            }

            return _replacements;
        }
    }
}
