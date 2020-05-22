using System.Collections.Generic;

namespace GeBoCommon.Utilities
{
    public class SimpleCache<TKey, TValue>
    {
        public delegate TValue CacheDataLoader(TKey key);

        private readonly Dictionary<TKey, TValue> _cache;
        private readonly CacheDataLoader _loader;

        public SimpleCache(CacheDataLoader loader)
        {
            _cache = new Dictionary<TKey, TValue>();
            _loader = loader;
        }

        public TValue Get(TKey key)
        {
            if (_cache.TryGetValue(key, out var cachedResult))
            {
                return cachedResult;
            }
            return _cache[key] = _loader(key);
        }

        public bool Remove(TKey key)
        {
            return _cache.Remove(key);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public int Count => _cache.Count;
    }
}
