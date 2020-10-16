using System;
using System.Diagnostics.CodeAnalysis;

#if (HS || PH || KK)
using System.Runtime.CompilerServices;
using System.Threading;

#endif

namespace GeBoCommon.Utilities
{
    /// <summary>
    /// Basic implementation of .NET Lazy
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
#if (HS || PH || KK)
    public class SimpleLazy<T>
    {
        private readonly object _basicLock;
        private readonly Func<T> _valueFactory;
        private object _value;

        public SimpleLazy(Func<T> valueFactory)
        {
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            _basicLock = new object();
        }

        public bool IsValueCreated => _value is StrongBox<T>;

        public T Value
        {
            get
            {
                StrongBox<T> realBox;
                if (_value is Exception tmpException)
                {
                    throw new MemberAccessException("Unexpected error", tmpException);
                }

                try
                {
                    if (!IsValueCreated)
                    {
                        lock (_basicLock)
                        {
                            if (!IsValueCreated)
                            {
                                Interlocked.CompareExchange(ref _value, new StrongBox<T>(_valueFactory()), null);
                            }
                        }
                    }

                    realBox = _value as StrongBox<T>;
                }
                catch (Exception e)
                {
                    _value = e;
                    throw;
                }

                if (realBox != null) return realBox.Value;
                var error = new MemberAccessException("error during lazy initialization");
                _value = error;
                throw error;
            }
        }

        public override string ToString()
        {
            return IsValueCreated ? Value.ToString() : "Value is not created.";
        }
    }
#else
    public class SimpleLazy<T>
    {
        private readonly Lazy<T> _real;

        public SimpleLazy(Func<T> valueFactory)
        {
            _real = new Lazy<T>(valueFactory, true);
        }

        public override string ToString() => _real.ToString();
        public bool IsValueCreated => _real.IsValueCreated;
        public T Value => _real.Value;
    }
#endif
}
