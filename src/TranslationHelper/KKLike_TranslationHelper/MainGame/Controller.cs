using ActionGame;

namespace TranslationHelperPlugin.MainGame
{
    partial class Controller
    {
        protected override void OnPeriodChange(Cycle.Type period)
        {
            TranslationHelper.RegistrationManager.Cleanup();
            Configuration.OnPeriodChange(this, new PeriodChangeEventArgs(period));

            base.OnPeriodChange(period);
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            TranslationHelper.RegistrationManager.Cleanup(true);
            Configuration.OnDayChange(this, new DayChangeEventArgs(day));
            base.OnDayChange(day);
        }
    }
}
