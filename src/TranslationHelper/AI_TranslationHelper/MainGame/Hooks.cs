using System;
using AIProject;
using HarmonyLib;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Hooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MerchantActor), nameof(MerchantActor.CharaName), MethodType.Getter)]
        internal static void MerchantActorCharaNameGetterPostfix(ref string __result)
        {
            try
            {
                __result = Configuration.MerchantCharaName.IsNullOrEmpty()
                    ? __result
                    : Configuration.MerchantCharaName;
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogWarning($"Unexpected error: {err.Message}");
                Logger.LogDebug(err);
            }
#pragma warning restore CA1031
        }
    }
}
