using System;
using GeBoCommon.Utilities;

namespace TranslationHelperPlugin.MainGame
{
    internal partial class Configuration
    {
        public static event EventHandler<PeriodChangeEventArgs> PeriodChange;
        public static event EventHandler<DayChangeEventArgs> DayChange;
        public static event EventHandler<HSceneEventArgs> StartH;
        public static event EventHandler<HSceneEventArgs> EndH;

        internal static void OnPeriodChange(object sender, PeriodChangeEventArgs eventArgs)
        {
            PeriodChange?.SafeInvoke(sender, eventArgs);
        }

        internal static void OnDayChange(object sender, DayChangeEventArgs eventArgs)
        {
            DayChange?.SafeInvoke(sender, eventArgs);
        }

        public static void OnStartH(object sender, HSceneEventArgs eventArgs)
        {
            StartH?.SafeInvoke(sender, eventArgs);
        }

        public static void OnEndH(object sender, HSceneEventArgs eventArgs)
        {
            TranslationHelper.RegistrationManager.Cleanup();
            EndH?.SafeInvoke(sender, eventArgs);
        }
    }
}
