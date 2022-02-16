using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using Studio;

namespace GeBoCommon.Studio
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class SceneUtils
    {
        private static readonly SimpleLazy<Func<SceneLoadScene, List<string>>> SceneLoadSceneListPathGetter =
            new SimpleLazy<Func<SceneLoadScene, List<string>>>(() =>
                Delegates.LazyReflectionInstanceGetter<SceneLoadScene, List<string>>("listPath"));

        [PublicAPI]
        public static readonly string StudioSceneRootFolder =
            PathUtils.NormalizePath(Path.Combine(UserData.Path, @"studio\scene")).ToLowerInvariant();

        [PublicAPI]
        public static List<string> GetSceneLoaderPaths(SceneLoadScene loader)
        {
            List<string> result = null;
            loader.SafeProc(l => result = SceneLoadSceneListPathGetter.Value(l));
            return result ?? new List<string>();
        }

        [UsedImplicitly]
        public static OCIChar GetMainChara(object instance)
        {
            return Traverse.Create(instance)?.Property("ociChar")?.GetValue<OCIChar>();
        }
    }
}
