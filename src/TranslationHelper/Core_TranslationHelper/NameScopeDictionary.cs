using System;
using System.Collections.Generic;

namespace TranslationHelperPlugin
{
    internal class NameScopeDictionary<T> where T : new()
    {
        private readonly Dictionary<NameScope, T> _dictionary = new Dictionary<NameScope, T>();
        private readonly Func<T> _initializer;
        private readonly object _lock = new object();

        public NameScopeDictionary(Func<T> initializer = null)
        {
            _initializer = initializer;
        }

        public T this[NameScope key]
        {
            get
            {
                lock (_lock)
                {
                    return _dictionary.TryGetValue(key, out var result) ? result : InitializeScope(key);
                }
            }
        }

        private T InitializeScope(NameScope scope)
        {
            var result = _initializer != null ? _initializer() : new T();
            lock (_lock)
            {
                _dictionary[scope] = result;
            }

            return result;
        }

        public IEnumerable<NameScope> GetScopes()
        {
            lock (_lock)
            {
                return _dictionary.Keys;
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
