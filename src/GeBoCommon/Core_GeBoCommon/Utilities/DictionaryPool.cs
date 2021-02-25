using System.Collections.Generic;
using BepInEx.Logging;

namespace GeBoCommon.Utilities
{
    public static class DictionaryPool<TKey, TValue>
    {
        private static readonly Dictionary<IEqualityComparer<TKey>, ObjectPool<Dictionary<TKey, TValue>>> Pools =
            new Dictionary<IEqualityComparer<TKey>, ObjectPool<Dictionary<TKey, TValue>>>();

        private static ManualLogSource Logger => GeBoAPI.Instance != null ? GeBoAPI.Instance.Logger : null;

        public static Dictionary<TKey, TValue> Get()
        {
            return Get(EqualityComparer<TKey>.Default);
        }

        private static ObjectPool<Dictionary<TKey, TValue>> GetPool(IEqualityComparer<TKey> equalityComparer)
        {
            if (Pools.TryGetValue(equalityComparer, out var pool)) return pool;

            Pools[equalityComparer] = pool = new ObjectPool<Dictionary<TKey, TValue>>(null, x => x.Clear(),
                () => new Dictionary<TKey, TValue>(equalityComparer));
            Logger?.DebugLogDebug(
                $"{typeof(DictionaryPool<TKey, TValue>).PrettyTypeName()}.{nameof(Get)}: created new pool: {pool.GetPrettyTypeFullName()}");

            return pool;
        }

        public static Dictionary<TKey, TValue> Get(IEqualityComparer<TKey> equalityComparer)
        {
            if (!GeBoAPI.EnableObjectPools) return new Dictionary<TKey, TValue>(equalityComparer);
            Logger?.DebugLogDebug(
                $"{typeof(DictionaryPool<TKey, TValue>).PrettyTypeFullName()}.{nameof(Get)}({equalityComparer}): start");
            return GetPool(equalityComparer).Get();
        }

        public static void Release(Dictionary<TKey, TValue> obj)
        {
            if (!GeBoAPI.EnableObjectPools) return;
            Logger?.DebugLogDebug($"{typeof(DictionaryPool<TKey, TValue>).PrettyTypeFullName()}.{nameof(Release)}: start");
            if (Pools.TryGetValue(obj.Comparer, out var pool))
            {
                pool.Release(obj);
                return;
            }

            Logger?.LogWarning(
                $"{typeof(DictionaryPool<TKey, TValue>).PrettyTypeFullName()}.{nameof(Release)}: Unable to find pool for {obj.Comparer}");
        }
    }
}
