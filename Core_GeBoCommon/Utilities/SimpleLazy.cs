using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace GeBoCommon.Utilities
{
#if (HS || PH || KK)
    public class SimpleLazy<T>
    {
        private object value = null;
        private readonly Func<T> valueFactory;
        private readonly object basicLock;

        public SimpleLazy(Func<T> valueFactory)
        {
            this.valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
            basicLock = new object();
        }

        public override string ToString()
        {
            return IsValueCreated ? Value.ToString() : "Value is not created.";
        }

        public bool IsValueCreated => value != null && value is StrongBox<T>;

        public T Value
        {
            get
            {
                StrongBox<T> realBox = null;
                if (value is Exception tmpException)
                {
                    throw new MemberAccessException("Unexpected error", tmpException);
                }
                try
                {
                    if (!IsValueCreated)
                    {
                        lock (basicLock)
                        {
                            if (!IsValueCreated)
                            {
                                Interlocked.CompareExchange(ref value, new StrongBox<T>(valueFactory()), null);
                            }
                        }
                    }
                    realBox = value as StrongBox<T>;
                }
                catch (Exception e)
                {
                    value = e;
                    throw;
                }

                if (realBox is null)
                {
                    var error = new MemberAccessException("error during lazy initialization");
                    value = error;
                    throw error;
                }
                return realBox.Value;
            }
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
