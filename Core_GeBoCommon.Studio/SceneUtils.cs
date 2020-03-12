using GeBoCommon.Utilities;
using HarmonyLib;
using Illusion.Game.Extensions;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GeBoCommon.Studio
{
    public static class SceneUtils
    {
        private static readonly FieldInfo SceneLoadScene_listPath = AccessTools.Field(typeof(SceneLoadScene), "listPath");

        public static readonly string StudioSceneRootFolder = PathUtils.NormalizePath(Path.Combine(UserData.Path, @"studio\scene")).ToLowerInvariant();

        public static List<string> GetSceneLoaderPaths(SceneLoadScene loader)
        {
            if (loader != null)
            {
                return new List<string>((IEnumerable<string>)SceneLoadScene_listPath.GetValue(loader));
            }
            return new List<string>();
        }

        public static OCIChar GetMainChara(object __instance)
        {
            return Traverse.Create(__instance).Property("ociChar").GetValue<OCIChar>();
        }
    }
}
