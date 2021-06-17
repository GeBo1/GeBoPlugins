using System;
using AIProject;
using GeBoCommon.Utilities;
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

            catch (Exception err)
            {
                Logger.LogException(err, nameof(MerchantActorCharaNameGetterPostfix));
            }
        }
    }
}
