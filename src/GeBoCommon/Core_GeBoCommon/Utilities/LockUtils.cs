using System.Threading;
using GeBoCommon.Utilities.LockWrappers;

namespace GeBoCommon.Utilities
{
    public static class LockUtils
    {
        public static ReaderWriterLockSlim CreateLock(
            LockRecursionPolicy recursionPolicy = LockRecursionPolicy.SupportsRecursion)
        {
            return new ReaderWriterLockSlim(recursionPolicy);
        }

        public static LockWrapper GetDisposableReadOnlyLock(this ReaderWriterLockSlim lockSlim)
        {
            return new ReadOnlyLockWrapper(lockSlim);
        }

        public static LockWrapper GetDisposableUpgradableReadLock(this ReaderWriterLockSlim lockSlim)
        {
            return new UpgradableReadLockWrapper(lockSlim);
        }

        public static LockWrapper GetDisposableWriteLock(this ReaderWriterLockSlim lockSlim)
        {
            return new WriteLockWrapper(lockSlim);
        }
    }
}
