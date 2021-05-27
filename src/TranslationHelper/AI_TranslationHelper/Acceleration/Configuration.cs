using System;
using System.Collections.Generic;
using ADV.Commands.Base;
using AIProject;
using AIProject.Animal;
using AIProject.UI;
using HarmonyLib;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Acceleration
{
    internal static partial class Configuration
    {
        internal static readonly Dictionary<string, string> ConfirmSceneSentenceTranslations =
            new Dictionary<string, string>();

        internal static readonly HashSet<string> ConfirmSceneSentenceHandled = new HashSet<string>();

        internal static void AI_GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            TranslationHelper.AccelerationBehaviorChanged += AI_AccelerationBehaviorChanged;
            ResetCaches();

            StringMethodTranspilerHelper.PatchMethod(harmony, typeof(WildGround), "InitializeCommandLabels");
            StringMethodTranspilerHelper.PatchMethod(harmony, typeof(Format), nameof(Format.Do));
            StringMethodTranspilerHelper.PatchMethod(harmony, typeof(FormatVAR), nameof(Format.Do));
            StringMethodTranspilerHelper.PatchMethod(harmony, typeof(AgentActor), "InitCommands");
            StringMethodTranspilerHelper.PatchMethod(harmony, typeof(PetHomeUI), "RemoveAnimal");
        }

        private static void AI_AccelerationBehaviorChanged(object sender, EventArgs e)
        {
            ResetCaches();
        }

        private static void ResetCaches()
        {
            ConfirmSceneSentenceHandled.Clear();
            ConfirmSceneSentenceTranslations.Clear();
        }
    }
}
