using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GeBoCommon.Utilities;

#if AI||HS2
using AIChara;
#endif

namespace StudioMultiSelectCharaPlugin
{
    public class CharaId : IEquatable<CharaId>, IComparable<CharaId>, IComparable
    {
        private readonly byte[] _value;

        private readonly SimpleLazy<string> _sortKey;
        private readonly SimpleLazy<int> _hashCode;
        public CharaId(byte[] bytes)
        {
            _value = bytes;
            _sortKey = new SimpleLazy<string>(() => Convert.ToBase64String(_value));
            _hashCode = new SimpleLazy<int>(() => GetSortKey().GetHashCode());
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

            return !arr1.Where((t, i) => t != arr2[i]).Any();
        }

        public bool Equals(CharaId other)
        {
            return !(other is null) && IsByteArrayEqual(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                return ReferenceEquals(this, obj) || Equals(obj as CharaId);
            }
            return false;
        }

        internal string GetSortKey() => _sortKey.Value;

        public override int GetHashCode() => _hashCode.Value;
        public int CompareTo(CharaId other)
        {
            return string.Compare(GetSortKey(), other.GetSortKey(), StringComparison.Ordinal);
        }

        public int CompareTo(object obj)
        {
            return obj is CharaId charaId ? CompareTo(charaId) : GetHashCode().CompareTo(obj);
        }

        public static bool operator ==(CharaId left, CharaId right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(CharaId left, CharaId right)
        {
            return !(left == right);
        }

        public static bool operator <(CharaId left, CharaId right)
        {
            return left is null ? (right != null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(CharaId left, CharaId right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(CharaId left, CharaId right)
        {
            return left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(CharaId left, CharaId right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
    }
}
