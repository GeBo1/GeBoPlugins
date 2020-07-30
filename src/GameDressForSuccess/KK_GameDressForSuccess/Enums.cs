using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GameDressForSuccessPlugin
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "User selectable option")]
    public enum PluginMode
    {
        [Description("Always change")]
        Always,

        [Description("Only if clothing type is auto")]
        AutomaticOnly
    }


    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "User selectable option")]
    public enum ResetToAutomaticMode
    {
        [Description("Default game behavior")]
        Never = 0,

        [Description("At the start of each day")]
        DayChange = 1,

        [Description("At the start of each period")]
        PeriodChange = 2
    }
}
