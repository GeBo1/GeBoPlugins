using System.Collections.Generic;

namespace GeBoCommon.Utilities
{
    public static class HashSetPool<T>
    {
        private static readonly ObjectPool<HashSet<T>> Pool = new ObjectPool<HashSet<T>>(null, x => x.Clear());

        public static HashSet<T> Get()
        {
            return !GeBoAPI.EnableObjectPools ? new HashSet<T>() : Pool.Get();
        }

        public static void Release(HashSet<T> obj)
        {
            if (GeBoAPI.EnableObjectPools) Pool.Release(obj);
        }
    }
}
