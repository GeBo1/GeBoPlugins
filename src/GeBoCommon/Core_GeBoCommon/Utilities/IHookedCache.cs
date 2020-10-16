using System.Diagnostics.CodeAnalysis;

namespace GeBoCommon.Utilities
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IHookedCacheEventArgs
    {
        bool Cancel { get; }
        object Target { get; }
    }

    public interface IHookedCache
    {
        void OnHookPrefix(IHookedCacheEventArgs e);
        void OnHookPostfix(IHookedCacheEventArgs e);
        object ConvertTargetToKey(object obj);
    }
}
