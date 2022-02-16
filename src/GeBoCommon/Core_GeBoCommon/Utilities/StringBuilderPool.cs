using System.Text;

namespace GeBoCommon.Utilities
{
    public static class StringBuilderPool
    {
        private static readonly ObjectPool<StringBuilder>
            Pool = new ObjectPool<StringBuilder>(null, ClearStringBuilder);

        private static void ClearStringBuilder(StringBuilder stringBuilder)
        {
            if (stringBuilder.Length < 512)
            {
                stringBuilder.Length = 0;
            }
            else
            {
                stringBuilder.Replace(stringBuilder.ToString(), string.Empty);
            }
        }

        public static StringBuilder Get()
        {
            return !GeBoAPI.EnableObjectPools ? new StringBuilder() : Pool.Get();
        }

        public static void Release(StringBuilder obj)
        {
            if (GeBoAPI.EnableObjectPools) Pool.Release(obj);
        }
    }
}
