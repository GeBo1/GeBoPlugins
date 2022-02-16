using BepInEx;
using JetBrains.Annotations;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    public static class PluginUtils
    {
        [PublicAPI]
        public static T InstanceGetter<T>(ref T cachedInstance) where T : BaseUnityPlugin
        {
            if (cachedInstance == null)
            {
                cachedInstance = Object.FindObjectOfType<T>();
            }

            return cachedInstance;
        }
    }
}
