using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace GeBoCommon.Studio
{
    public static class SceneUtils
    {
        private static readonly FieldInfo SceneLoadSceneListPath = AccessTools.Field(typeof(SceneLoadScene), "listPath");

        public static readonly string StudioSceneRootFolder = PathUtils.NormalizePath(Path.Combine(UserData.Path, @"studio\scene")).ToLowerInvariant();

        public static List<string> GetSceneLoaderPaths(SceneLoadScene loader)
        {
            return loader != null ? new List<string>((IEnumerable<string>)SceneLoadSceneListPath.GetValue(loader)) : new List<string>();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Utility class, game differences")]
        public static OCIChar GetMainChara(object instance)
        {
            return Traverse.Create(instance).Property("ociChar").GetValue<OCIChar>();
        }
    }
}
