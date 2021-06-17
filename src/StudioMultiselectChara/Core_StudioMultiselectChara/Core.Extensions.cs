using System;
using System.Collections.Generic;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Chara;
using Studio;
using GeBoCommon.Studio;

#if AI||HS2
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    [PublicAPI]
    public static class Extensions
    {

        public static CharaId GetMatchId(this ChaFile chaFile)
        {
            return CharaIdCache.Get(chaFile);
        }

        public static CharaId GetMatchId(this OCIChar ociChar)
        {
            return ociChar?.oiCharInfo.charFile.GetMatchId();
        }

        #region CharaId cache

        private static readonly SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>> LazyCharaIdCache =
            new SimpleLazy<HookedSimpleCache<ChaFile, CharaId, ChaControl>>(() =>
            {
                var cache = new HookedSimpleCache<ChaFile, CharaId, ChaControl>(IdLoader, CacheConverter, true, true);
                CharacterApi.CharacterReloaded += (sender, e) =>
                {
                    e.ReloadedCharacter.SafeProc(rc => cache.Remove(rc.chaFile));
                };
                return cache;
            });


        internal static HookedSimpleCache<ChaFile, CharaId, ChaControl> CharaIdCache => LazyCharaIdCache.Value;

        private static ChaFile CacheConverter(ChaControl chaControl)
        {
            return chaControl == null ? null : chaControl.chaFile;
        }

        internal static TResult CacheWrapper<T, TResult>(this T obj, Dictionary<T, TResult> cache,
            Func<T, TResult> loader)
        {
            if (cache.TryGetValue(obj, out var cachedResult))
            {
                return cachedResult;
            }

            return cache[obj] = loader(obj);
        }

        private static CharaId IdLoader(ChaFile chaFile)
        {
            return new CharaId(chaFile);
        }

        #endregion CharaId cache
    }
}
