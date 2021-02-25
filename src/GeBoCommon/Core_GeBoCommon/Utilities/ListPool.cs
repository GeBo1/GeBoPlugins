using System.Collections.Generic;

namespace GeBoCommon.Utilities
{
    public static class ListPool<T>
    {
        private static readonly ObjectPool<List<T>> Pool = new ObjectPool<List<T>>(null, x => x.Clear());

        public static List<T> Get()
        {
            return !GeBoAPI.EnableObjectPools ? new List<T>() : Pool.Get();
        }

        public static void Release(List<T> obj)
        {
            if (GeBoAPI.EnableObjectPools) Pool.Release(obj);
        }
    }
}
