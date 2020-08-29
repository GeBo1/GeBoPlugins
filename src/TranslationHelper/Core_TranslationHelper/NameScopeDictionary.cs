using System.Collections.Generic;

namespace TranslationHelperPlugin
{
    internal class NameScopeDictionary<T> where T : new()
    {
        private readonly Dictionary<NameScope, T> _dictionary = new Dictionary<NameScope, T>();

        public T this[NameScope key]
        {
            get
            {
                if (!_dictionary.TryGetValue(key, out var result))
                {
                    result = _dictionary[key] = new T();
                }

                return result;
            }
        }

        public void Clear()
        {
            _dictionary.Clear();
        }
    }
}
