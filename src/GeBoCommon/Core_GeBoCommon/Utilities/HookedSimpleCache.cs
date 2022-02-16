using System;
using System.Collections.Generic;
using System.ComponentModel;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeBoCommon.Utilities
{
    public sealed class HookedSimpleCache<TKey, TValue, THookTarget> : SimpleCache<TKey, TValue>, IHookedCache
        where THookTarget : MonoBehaviour
    {
        public delegate TKey HookConverter(THookTarget hookTarget);

        private readonly HookConverter _convertTargetToKey;

        public HookedSimpleCache(CacheDataLoader loader, HookConverter converter, bool useDefaultRemovalHook = false,
            bool emptyCacheOnSceneChange = false, string cacheName = null) : base(loader, cacheName)
        {
            _convertTargetToKey = converter;
            EmptyCacheOnSceneChange = emptyCacheOnSceneChange;
            UseDefaultRemovalHook = useDefaultRemovalHook;

            var hookTarget = AccessTools.Method(typeof(THookTarget), "OnDestroy", ObjectUtils.GetEmptyArray<Type>());
            if (hookTarget is null)
            {
                throw new ArgumentException($"unable to hook OnDestroy for {typeof(THookTarget)})");
            }

            var prefix = new HarmonyMethod(GetType(), nameof(PrefixHook));
            var postfix = new HarmonyMethod(GetType(), nameof(PostfixHook));

            if (prefix is null)
            {
                throw new Exception("Unable to wrap prefix");
            }

            if (postfix is null)
            {
                throw new Exception("Unable to wrap postfix");
            }

            var harmony = Harmony.CreateAndPatchAll(GetType());
            harmony.Patch(hookTarget, prefix, postfix);

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            if (UseDefaultRemovalHook)
            {
                HookPrefix += DefaultRemovalHook;
            }

            HookedSimpleCacheState.RegisterCache(typeof(THookTarget), this);
        }

        // if true clear instance on activeSceneChanged
        public bool EmptyCacheOnSceneChange { get; }

        // if true remove from this cache instance when the parent THookTarget's OnDestroy fires.
        public bool UseDefaultRemovalHook { get; }

        object IHookedCache.ConvertTargetToKey(object obj)
        {
            return _convertTargetToKey((THookTarget)obj);
        }

        public void OnHookPrefix(IHookedCacheEventArgs e)
        {
            OnHookPrefix((HookedSimpleCacheEventArgs<TKey>)e);
        }

        public void OnHookPostfix(IHookedCacheEventArgs e)
        {
            OnHookPostfix((HookedSimpleCacheEventArgs<TKey>)e);
        }

        [PublicAPI]
        public event EventHandler<HookedSimpleCacheEventArgs<TKey>> HookPrefix;

        [PublicAPI]
        public event EventHandler<HookedSimpleCacheEventArgs<TKey>> HookPostfix;

        private void DefaultRemovalHook(object sender, HookedSimpleCacheEventArgs<TKey> e)
        {
            Remove(e.Target);
            if (HookedSimpleCacheState.NeedsCleanup(typeof(THookTarget)))
            {
                HookedSimpleCacheState.Cleanup(typeof(THookTarget));
            }
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            if (!EmptyCacheOnSceneChange || Count == 0) return;
            Logger?.DebugLogDebug($"{this}: Clearing {Count} entries on activeSceneChanged");
            Clear();
        }

        public void OnHookPrefix(HookedSimpleCacheEventArgs<TKey> e)
        {
            HookPrefix?.SafeInvoke(this, e);
        }

        public void OnHookPostfix(HookedSimpleCacheEventArgs<TKey> e)
        {
            HookPostfix?.SafeInvoke(this, e);
        }

        internal static void PrefixHook(THookTarget __instance)
        {
            foreach (var cache in HookedSimpleCacheState.GetRegisteredCaches(typeof(THookTarget)))
            {
                var key = cache.ConvertTargetToKey(__instance);
                cache.OnHookPrefix(new HookedSimpleCacheEventArgs<TKey>((TKey)key));
            }
        }

        internal static void PostfixHook(THookTarget __instance)
        {
            foreach (var cache in HookedSimpleCacheState.GetRegisteredCaches(typeof(THookTarget)))
            {
                var key = cache.ConvertTargetToKey(__instance);
                cache.OnHookPostfix(new HookedSimpleCacheEventArgs<TKey>((TKey)key));
            }
        }
    }

    public class HookedSimpleCacheEventArgs<TEventTarget> : CancelEventArgs, IHookedCacheEventArgs
    {
        public HookedSimpleCacheEventArgs(TEventTarget target)
        {
            Cancel = false;
            Target = target;
        }

        public TEventTarget Target { get; }
        object IHookedCacheEventArgs.Target => Target;
    }

    internal static class HookedSimpleCacheState
    {
        internal static readonly Dictionary<string, List<WeakReference>> CacheRegistry =
            new Dictionary<string, List<WeakReference>>();

        internal static readonly HashSet<string> NeedsCleanupSet = new HashSet<string>();

        private static string TypeToKey(Type typ)
        {
            return typ.AssemblyQualifiedName;
        }

        internal static IEnumerable<IHookedCache> GetRegisteredCaches(Type typ)
        {
            return GetRegisteredCaches(TypeToKey(typ));
        }

        private static IEnumerable<IHookedCache> GetRegisteredCaches(string key)
        {
            if (!CacheRegistry.TryGetValue(key, out var registeredCaches)) yield break;

            var dirty = false;
            foreach (var cache in registeredCaches)
            {
                if (cache.IsAlive)
                {
                    yield return (IHookedCache)cache.Target;
                }
                else
                {
                    dirty = true;
                }
            }

            if (dirty)
            {
                NeedsCleanupSet.Add(key);
            }
        }

        internal static void RegisterCache(Type typ, IHookedCache cache)
        {
            var key = TypeToKey(typ);
            if (!CacheRegistry.TryGetValue(key, out var registeredCaches))
            {
                CacheRegistry[key] = registeredCaches = new List<WeakReference>();
            }

            registeredCaches.Add(new WeakReference(cache));
        }

        [UsedImplicitly]
        internal static bool IsRegistered(Type typ)
        {
            return IsRegistered(TypeToKey(typ));
        }

        private static bool IsRegistered(string key)
        {
            return CacheRegistry.ContainsKey(key);
        }

        internal static bool NeedsCleanup(Type typ)
        {
            return NeedsCleanup(TypeToKey(typ));
        }

        private static bool NeedsCleanup(string key)
        {
            return IsRegistered(key) && NeedsCleanupSet.Contains(key);
        }

        internal static void Cleanup(Type typ)
        {
            Cleanup(TypeToKey(typ));
        }

        private static void Cleanup(string key)
        {
            if (!IsRegistered(key))
            {
                return;
            }

            CacheRegistry[key].RemoveAll(c => !c.IsAlive);
            NeedsCleanupSet.Remove(key);
        }
    }
}
