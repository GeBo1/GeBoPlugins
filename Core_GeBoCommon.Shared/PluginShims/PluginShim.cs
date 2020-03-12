using BepInEx;
using BepInEx.Bootstrap;
using System.Collections.Generic;

namespace GeBoCommon.PluginShims
{
    public abstract class PluginShim<T> where T : class, new()

    {
        protected abstract IEnumerable<string> PluginGUIDs { get; }

        private bool initialized = false;
        private PluginInfo _pluginInfo = null;

        protected PluginInfo PluginInfo
        {
            get
            {
                if (!initialized)
                {
                    foreach (string pluginGUID in PluginGUIDs)
                    {
                        if (Chainloader.PluginInfos.TryGetValue(pluginGUID, out PluginInfo match))
                        {
                            _pluginInfo = match;
                            break;
                        }
                    }
                    initialized = true;
                }
                return _pluginInfo;
            }
        }

        private static T _instance = null;
        public static T Instance => _instance ?? (_instance = new T());

        /*
        protected void SetupLazyDelegate<T>(string fieldName, Func<PluginShim, T> setupDelagate) where T: System.Delegate
        {
            FieldInfo fieldInfo = AccessTools.Field(this.GetType(), fieldName);
            T initial = (T)System.Delegate.CreateDelegate(typeof(T), setupDelagate.Method, () =>
            {
                T realDelagate = setupDelagate(this);
                fieldInfo.SetValue(this, realDelagate);
                return realDelagate.DynamicInvoke(args);
            });
        }
        */
    }
}
