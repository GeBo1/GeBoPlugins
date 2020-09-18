using System.Collections.Generic;

namespace TranslationHelperPlugin
{
    internal class NameScopeDictionary<T> where T : new()
    {
        private readonly Dictionary<NameScope, T> _dictionary = new Dictionary<NameScope, T>();
        private readonly object _lock = new object();

        public T this[NameScope key]
        {
            get
            {
                T result;
                lock (_lock)
                {
                    if (!_dictionary.TryGetValue(key, out result))
                    {
                        _dictionary[key] = result = new T();
                    }
                }
                return result;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _dictionary.Clear();
            }
        }
    }
}
