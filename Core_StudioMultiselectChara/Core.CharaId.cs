using System;
using System.Collections.Generic;
using System.Linq;
#if AI
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    public class CharaId : IEquatable<CharaId>, IComparable<CharaId>, IComparable
    {
        private readonly byte[] value;

        private int? hashCode = null;

        private string sortKey = null;

        public CharaId(byte[] bytes)
        {
            value = bytes;
        }

        public CharaId(IEnumerable<byte> bytes) : this(bytes.ToArray())
        {
        }

        public CharaId(ChaFile chaFile) : this(chaFile?.GetParameterBytes())
        {
        }

        private static bool IsByteArrayEqual(byte[] arr1, byte[] arr2)
        {
            // faster than SequenceEqual
            if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(CharaId other)
        {
            return other is object && IsByteArrayEqual(value, other.value);
        }

        public override bool Equals(object obj)
        {
            if (obj is object)
            {
                return ReferenceEquals(this, obj) || Equals(obj as CharaId);
            }
            return false;
        }

        internal string GetSortKey()
        {
            return sortKey ?? (sortKey = Convert.ToBase64String(value));
        }

        public override int GetHashCode()
        {
            return (hashCode ?? (hashCode = new int?(GetSortKey().GetHashCode()))).Value;
        }

        public int CompareTo(CharaId other)
        {
            return GetSortKey().CompareTo(other.GetSortKey());
        }

        public int CompareTo(object obj)
        {
            return obj is CharaId charaId ? CompareTo(charaId) : GetHashCode().CompareTo(obj);
        }

        public static bool operator ==(CharaId left, CharaId right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(CharaId left, CharaId right)
        {
            return !(left == right);
        }

        public static bool operator <(CharaId left, CharaId right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CharaId left, CharaId right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CharaId left, CharaId right)
        {
            return left is object && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CharaId left, CharaId right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}