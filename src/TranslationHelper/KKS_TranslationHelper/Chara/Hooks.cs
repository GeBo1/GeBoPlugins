using System;
using GeBoCommon.Utilities;
using HarmonyLib;

namespace TranslationHelperPlugin.Chara
{
    internal partial class Hooks
    {
        internal static void KKS_SetupHooks(Harmony harmony)
        {
            FullNamePropertyHooker<ChaFileParameter>.HookFullnameProperty(harmony, "fullname");
            FullNamePropertyHooker<ChaFilePreview>.HookFullnameProperty(harmony, "fullname");
        }

        private static class FullNamePropertyHooker<T>
        {
            private static readonly Func<T, string> FirstNameGetter =
                Delegates.LazyReflectionInstanceGetter<T, string>("firstname");

            private static readonly Func<T, string> LastNameGetter =
                Delegates.LazyReflectionInstanceGetter<T, string>("lastname");

            private static bool FullNameGetterPrefix(T __instance, ref string __result)
            {
                try
                {
                    if (!TranslationHelper.ShowGivenNameFirst) return true;
                    __result = string.Join(TranslationHelper.SpaceJoiner, FirstNameGetter(__instance),
                        LastNameGetter(__instance));
                    return false;
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(FullNameGetterPrefix));
                }

                return true;
            }

            internal static void HookFullnameProperty(Harmony harmony, string propertyName)
            {
                var propGetter = AccessTools.PropertyGetter(typeof(T), propertyName);
                if (propGetter == null)
                {
                    Logger.LogWarning($"Unable to find getter for {typeof(T).PrettyTypeName()}.{propertyName}");
                    return;
                }

                var prefix = AccessTools.Method(typeof(FullNamePropertyHooker<T>), nameof(FullNameGetterPrefix));
                if (prefix == null)
                {
                    Logger.LogWarning($"Unable find {nameof(FullNameGetterPrefix)}");
                    return;
                }

                try
                {
                    harmony.Patch(propGetter, new HarmonyMethod(prefix));
                    Logger.LogDebug($"Hooked {typeof(T).PrettyTypeName()}.{propertyName} getter");
                }
                catch (Exception err)
                {
                    Logger.LogException(err,
                        $"{nameof(KKS_SetupHooks)}: unable to hook fullname support for {typeof(T).PrettyTypeName()}");
                }
            }
        }
    }
}
