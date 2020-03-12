using System;
using System.Collections;
using System.Collections.Generic;

namespace GeBoCommon.Utilities
{
    public class SimpleCache<TKey, TValue>
    {
        public delegate TValue CacheDataLoader(TKey key);

        private readonly Dictionary<TKey, TValue> Cache;
        private readonly CacheDataLoader Loader;

        public SimpleCache(CacheDataLoader loader)
        {
            Cache = new Dictionary<TKey, TValue>();
            Loader = loader;
        }

        public TValue Get(TKey key)
        {
            if (Cache.TryGetValue(key, out TValue cachedResult))
            {
                return cachedResult;
            }
            return Cache[key] = Loader(key);
        }

        public bool Remove(TKey key)
        {
            return Cache.Remove(key);
        }

        public void Clear()
        {
            Cache.Clear();
        }

        public int Count => Cache.Count;
    }
}