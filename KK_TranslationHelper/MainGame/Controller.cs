using ActionGame;
using BepInEx.Logging;
using KKAPI.MainGame;

namespace TranslationHelperPlugin.MainGame
{
    public class Controller : GameCustomFunctionController
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        /*
        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            Logger.LogDebug($"{GetType().FullName}.{nameof(OnGameLoad)}");
            base.OnGameLoad(args);
        }
        */

        protected override void OnPeriodChange(Cycle.Type period)
        {
            Logger.LogDebug($"{GetType().FullName}.{nameof(OnPeriodChange)}");
            TranslationHelper.RegistrationManager.Cleanup();
            base.OnPeriodChange(period);
        }

        protected override void OnEnterNightMenu()
        {
            Logger.LogDebug($"{GetType().FullName}.{nameof(OnEnterNightMenu)}");
            TranslationHelper.RegistrationManager.Cleanup(true);
            base.OnEnterNightMenu();
        }

        /*
        protected override void OnStartH(HSceneProc proc, bool freeH)
        {
            Logger.LogDebug($"{GetType().FullName}.{nameof(OnStartH)}");
            base.OnStartH(proc, freeH);
        }
        */

        protected override void OnEndH(HSceneProc proc, bool freeH)
        {
            Logger.LogDebug($"{GetType().FullName}.{nameof(OnEndH)}");
            TranslationHelper.RegistrationManager.Cleanup();
            base.OnEndH(proc, freeH);
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            Logger.LogDebug($"{GetType().FullName}.{nameof(OnDayChange)}");
            TranslationHelper.RegistrationManager.Cleanup(true);
            base.OnDayChange(day);
        }
    }
}
