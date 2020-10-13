using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ActionGame;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using KKAPI.MainGame;

namespace GameDressForSuccessPlugin
{
    class DressForSuccessController : GameCustomFunctionController
    {
        protected static ManualLogSource Logger =>
            GameDressForSuccess.Instance != null ? GameDressForSuccess.Instance.Logger : null;

        protected override void OnPeriodChange(Cycle.Type period)
        {
            Logger?.DebugLogDebug($"{GetType().FullName}.{nameof(OnPeriodChange)}({period})");
            if (GameDressForSuccess.ResetToAutomatic.Value >= ResetToAutomaticMode.PeriodChange)
            {
                GameDressForSuccess.Instance.SafeProcObject(o => o.SetPlayerClothesToAutomatic());
            }
            base.OnPeriodChange(period);
        }



        protected override void OnEnterNightMenu()
        {
            // Doing in both OnEnterNightMenu and OnDayChange to handle before saving and after loading
            Logger?.DebugLogDebug($"{GetType().FullName}.{nameof(OnEnterNightMenu)}()");
            if (GameDressForSuccess.ResetToAutomatic.Value >= ResetToAutomaticMode.DayChange)
            {
                GameDressForSuccess.Instance.SafeProcObject(o => o.SetPlayerClothesToAutomatic());
            }
            base.OnEnterNightMenu();
        }

        protected override void OnDayChange(Cycle.Week day)
        {
            // Doing in both OnEnterNightMenu and OnDayChange to handle before saving and after loading
            Logger?.DebugLogDebug($"{GetType().FullName}.{nameof(OnDayChange)}({day})");
            if (GameDressForSuccess.ResetToAutomatic.Value >= ResetToAutomaticMode.DayChange)
            {
                GameDressForSuccess.Instance.SafeProcObject(o => o.SetPlayerClothesToAutomatic());
            }
            base.OnDayChange(day);
        }

    }
}
