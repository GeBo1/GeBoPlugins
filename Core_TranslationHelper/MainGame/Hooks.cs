using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;

namespace TranslationHelperPlugin.MainGame
{
    partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        internal static Harmony SetupHooks()
        {
            return HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
