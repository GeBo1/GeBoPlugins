using System.ComponentModel;

namespace GameDressForSuccessPlugin
{
    public enum PluginMode
    {
        [Description("Always change")]
        Always,

        [Description("Only if clothing type is auto")]
        AutomaticOnly
    }
}
