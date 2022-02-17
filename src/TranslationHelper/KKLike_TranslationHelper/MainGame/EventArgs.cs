using System;
using ActionGame;
using JetBrains.Annotations;
using UnityEngine;

namespace TranslationHelperPlugin.MainGame
{
    public class PeriodChangeEventArgs : EventArgs
    {
        public PeriodChangeEventArgs(Cycle.Type period)
        {
            Period = period;
        }

        [PublicAPI]
        public Cycle.Type Period { get; }
    }

    public class DayChangeEventArgs : EventArgs
    {
        public DayChangeEventArgs(Cycle.Week day)
        {
            Day = day;
        }

        [PublicAPI]
        public Cycle.Week Day { get; }
    }

    public class HSceneEventArgs : EventArgs
    {
        public HSceneEventArgs(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            HProc = proc;
            Flag = hFlag;
            IsVR = vr;
        }

        [PublicAPI]
        public MonoBehaviour HProc { get; }

        [PublicAPI]
        public HFlag Flag { get; }

        [PublicAPI]
        public bool IsVR { get; }
    }
}
