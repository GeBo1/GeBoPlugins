using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI.Chara;
using KKAPI.Maker;

#if AI||HS2
using AIChara;
#endif

namespace TranslationHelperPlugin.Chara
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal const string GUID = TranslationHelper.GUID + ".chara";

        internal static bool TrackCharaFileControlPaths = true;

        internal static Dictionary<string, string> ChaFileControlPaths =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();
            CharacterApi.RegisterExtraBehaviour<Controller>(GUID);
            GameSpecificSetup(harmony);

            MakerAPI.MakerStartedLoading += MakerStartedLoading;
            MakerAPI.MakerExiting += MakerExiting;
        }

        private static void MakerExiting(object sender, EventArgs e)
        {
            ChaFileControlPaths.Clear();
        }

        private static void MakerStartedLoading(object sender, RegisterCustomControlsEvent e)
        {
            ChaFileControlPaths.Clear();
        }

        public static void TrackCharaFileControlPath(ChaFile chaFile, string filename)
        {
            try
            {
                filename = PathUtils.NormalizePath(filename);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
            {
                // not trackable
                return;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            var key = GetTrackingKey(chaFile, filename);
            ChaFileControlPaths[key] = filename;
        }

        [PublicAPI]
        public static void UntrackCharaFileControlPath(ChaFile chaFile, string filename)
        {
            ChaFileControlPaths.Remove(GetTrackingKey(chaFile, filename));
        }

        [PublicAPI]
        public static void UntrackCharaFileControlPath(ChaFile chaFile)
        {
            ChaFileControlPaths.Remove(GetTrackingKey(chaFile, chaFile.charaFileName));
        }

        public static bool TryGetCharaFileControlPath(ChaFile chaFile, string filename, out string result)
        {
            var key = GetTrackingKey(chaFile, filename);
            return ChaFileControlPaths.TryGetValue(key, out result);
        }

        public static bool TryGetCharaFileControlPath(ChaFile chaFile, out string result)
        {
            return TryGetCharaFileControlPath(chaFile, chaFile.charaFileName, out result);
        }

        private static string GetTrackingKey(ChaFile chaFile, string filename)
        {
            return StringUtils.JoinStrings("|||", chaFile.GetRegistrationID(), Path.GetFileName(filename));
        }
    }
}
