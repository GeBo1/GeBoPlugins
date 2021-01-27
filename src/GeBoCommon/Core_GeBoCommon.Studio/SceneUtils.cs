using System;
using System.Collections.Generic;
using System.IO;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using Studio;

namespace GeBoCommon.Studio
{
    public static class SceneUtils
    {
        private static readonly SimpleLazy<Func<SceneLoadScene, List<string>>> SceneLoadSceneListPathGetter =
            new SimpleLazy<Func<SceneLoadScene, List<string>>>(() =>
                Delegates.LazyReflectionInstanceGetter<SceneLoadScene, List<string>>("listPath"));

        public static readonly string StudioSceneRootFolder =
            PathUtils.NormalizePath(Path.Combine(UserData.Path, @"studio\scene")).ToLowerInvariant();

        public static List<string> GetSceneLoaderPaths(SceneLoadScene loader)
        {
            return loader != null ? new List<string>(SceneLoadSceneListPathGetter.Value(loader)) : new List<string>();
        }

        [UsedImplicitly]
        public static OCIChar GetMainChara(object instance)
        {
            return Traverse.Create(instance).Property("ociChar").GetValue<OCIChar>();
        }
    }
}
