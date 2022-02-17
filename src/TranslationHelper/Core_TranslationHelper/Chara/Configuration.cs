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

        // ReSharper disable once ConvertToConstant.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        internal static bool TrackCharaFileControlPaths = true;

        internal static readonly Dictionary<string, string> ChaFileControlPaths =
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
            Logger.LogDebug($"{typeof(Configuration).PrettyTypeFullName()}.{nameof(MakerExiting)}: start");
            ChaFileControlPaths.Clear();
            Logger.LogDebug($"{typeof(Configuration).PrettyTypeFullName()}.{nameof(MakerExiting)}: end");
        }

        private static void MakerStartedLoading(object sender, RegisterCustomControlsEvent e)
        {
            ChaFileControlPaths.Clear();
        }

        private static string GetNormalizedCharaFileControlPath(ChaFile chaFile, string filename)
        {
            var result = filename;
            if (chaFile is ChaFileControl chaFileControl && Path.GetDirectoryName(filename).IsNullOrEmpty())
            {
                result = chaFileControl.ConvertCharaFilePath(filename, chaFileControl.parameter.sex);
            }

            return PathUtils.NormalizePath(result);
        }

        public static void TrackCharaFileControlPath(ChaFile chaFile, string filename,
            Action<string> normalizedPathCallback = null)
        {
            try
            {
                filename = GetNormalizedCharaFileControlPath(chaFile, filename);
            }
            catch
            {
                // not trackable
                return;
            }

            var key = GetTrackingKey(chaFile, filename);
            ChaFileControlPaths[key] = filename;
            normalizedPathCallback?.Invoke(filename);
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
