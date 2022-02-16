using System;
using System.Threading;
using BepInEx;

namespace GeBoCommon.Utilities.LockWrappers
{
    public abstract class LockWrapper : IDisposable
    {
        private readonly bool _lockTaken;
        protected readonly ReaderWriterLockSlim Lock;
        protected readonly bool OnMainThread;


        public LockWrapper(ReaderWriterLockSlim lockSlim)
        {
            Lock = lockSlim;
            if (!NeedLock()) return;
            _lockTaken = false;
            OnMainThread = !ThreadingHelper.Instance.InvokeRequired;
            while (!_lockTaken)
            {
                _lockTaken = TryTakeLock();
                if (!OnMainThread && !_lockTaken) Thread.Sleep(0);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_lockTaken) ReleaseLock();
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        protected abstract bool NeedLock();
        protected abstract bool TryTakeLock();
        protected abstract void ReleaseLock();
    }

    internal class ReadOnlyLockWrapper : LockWrapper
    {
        internal ReadOnlyLockWrapper(ReaderWriterLockSlim lockSlim) : base(lockSlim) { }

        protected override bool NeedLock()
        {
            return Lock.IsReadLockHeld;
        }

        protected override void ReleaseLock()
        {
            Lock.ExitReadLock();
        }

        protected override bool TryTakeLock()
        {
            return Lock.TryEnterReadLock(1);
        }
    }


    internal class UpgradableReadLockWrapper : LockWrapper
    {
        internal UpgradableReadLockWrapper(ReaderWriterLockSlim lockSlim) : base(lockSlim) { }

        protected override bool NeedLock()
        {
            return Lock.IsUpgradeableReadLockHeld;
        }

        protected override void ReleaseLock()
        {
            Lock.ExitUpgradeableReadLock();
        }

        protected override bool TryTakeLock()
        {
            return Lock.TryEnterUpgradeableReadLock(1);
        }
    }

    internal class WriteLockWrapper : LockWrapper
    {
        internal WriteLockWrapper(ReaderWriterLockSlim lockSlim) : base(lockSlim) { }

        protected override bool NeedLock()
        {
            return Lock.IsWriteLockHeld;
        }

        protected override void ReleaseLock()
        {
            Lock.ExitWriteLock();
        }

        protected override bool TryTakeLock()
        {
            return Lock.TryEnterWriteLock(1);
        }
    }
}
