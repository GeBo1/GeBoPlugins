using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using JetBrains.Annotations;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using Studio;
using TranslationHelperPlugin.Chara;
using IllusionStudio = Studio.Studio;

#if AI || HS2
using AIChara;

#endif


namespace TranslationHelperPlugin.Studio
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal static readonly List<TryAlternateStudioCharaLoaderTranslation> AlternateStudioCharaLoaderTranslators =
            new List<TryAlternateStudioCharaLoaderTranslation>();

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (!StudioAPI.InsideStudio) return;
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;

            StudioSaveLoadApi.SceneLoad += StudioSaveLoadApi_SceneLoad;

            GameSpecificSetup(harmony);
        }

        private static void StudioSaveLoadApi_SceneLoad(object sender, SceneLoadEventArgs e)
        {
           if (e.Operation == SceneOperationKind.Clear) TranslationHelper.NotifyBehaviorChanged(e);
        }

        private static void CharacterApi_CharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            e?.ReloadedCharacter.SafeProcObject(UpdateTreeForChar);
        }

        internal static void UpdateTreeForChar(ChaControl chaControl)
        {
            UpdateTreeForChar(chaControl, null);
        }

        internal static void UpdateTreeForChar(ChaControl chaControl, Action<string> callback)
        {
            chaControl.SafeProcObject(c=>UpdateTreeForChar(c.chaFile, callback));
        }

        [UsedImplicitly]
        internal static void UpdateTreeForChar(ChaFile chaFile)
        {
            UpdateTreeForChar(chaFile, null);
        }

        internal static void UpdateTreeForChar(ChaFile chaFile, Action<string> callback)
        {
            if (chaFile == null) return;
            void Handler(string fullName)
            {
                if (string.IsNullOrEmpty(fullName)) return;
                var match = Singleton<IllusionStudio>.Instance.dicInfo
                    .Where(e => e.Value is OCIChar ociChar && ociChar.charInfo.chaFile == chaFile)
                    .Select(e => e.Key).FirstOrDefault();
                if (match == null) return;
                match.textName = fullName;
                callback?.Invoke(fullName);
            }

            chaFile.TranslateFullName(Handler);
        }
    }
}
