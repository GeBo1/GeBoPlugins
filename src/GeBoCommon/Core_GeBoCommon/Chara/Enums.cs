using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GeBoCommon.Chara
{
    [SerializableAttribute]
    public enum NameType
    {
        Unclassified = 0,
        Given = 1,
        Family = 2
    }

    [SerializableAttribute]
    public enum CharacterSex
    {
        Unspecified = -1,
        Male = 0,
        Female = 1
    }

    public class EnumEqualityComparer<T> : IEqualityComparer<T> where T : struct
    {
        public static readonly EnumEqualityComparer<T> Comparer = new EnumEqualityComparer<T>();
        private readonly Func<T, int> _converter;

        private EnumEqualityComparer()
        {
            var supportedTypes = new HashSet<Type>
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong)
            };

            var typeToCompare = typeof(T);
            if (!typeToCompare.IsEnum) throw new NotSupportedException($"{typeToCompare.Name} is not an enum");
            var underlyingType = Enum.GetUnderlyingType(typeToCompare);
            if (!supportedTypes.Contains(underlyingType))
            {
                throw new NotSupportedException(
                    $"{typeToCompare.Name} has unsupported underlying type: {underlyingType}");
            }


            var param = Expression.Parameter(typeToCompare, null);

            _converter = Expression.Lambda<Func<T, int>>(
                Expression.ConvertChecked(param, underlyingType), param).Compile();
        }

        public bool Equals(T x, T y)
        {
            return _converter(x) == _converter(y);
        }

        public int GetHashCode(T obj)
        {
            return _converter(obj);
        }
    }
}
