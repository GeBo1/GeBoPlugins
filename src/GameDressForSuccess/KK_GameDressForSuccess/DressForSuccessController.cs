using ActionGame;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using KKAPI.MainGame;

namespace GameDressForSuccessPlugin
{
    internal class DressForSuccessController : GameCustomFunctionController
    {
        protected static ManualLogSource Logger =>
            GameDressForSuccess.Instance != null ? GameDressForSuccess.Instance.Logger : null;

        private static void HandleResetToAutomatic(ResetToAutomaticMode minimumMode)
        {
            if (GameDressForSuccess.ResetToAutomatic.Value < minimumMode) return;
            GameDressForSuccess.Instance.SafeProcObject(o => o.SetPlayerClothesToAutomatic());
        }

        protected override void OnPeriodChange(Cycle.Type period)
        {
            Logger?.DebugLogDebug($"{GetType().FullName}.{nameof(OnPeriodChange)}({period})");
            HandleResetToAutomatic(ResetToAutomaticMode.PeriodChange);
            base.OnPeriodChange(period);
        }
        
        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            Logger?.DebugLogDebug($"{GetType().FullName}.{nameof(OnGameSave)}()");
            HandleResetToAutomatic(ResetToAutomaticMode.DayChange);
            base.OnGameSave(args);
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            Logger?.DebugLogDebug($"{GetType().FullName}.{nameof(OnDayChange)}({day})");
            HandleResetToAutomatic(ResetToAutomaticMode.DayChange);
            base.OnDayChange(day);
        }
    }
}
