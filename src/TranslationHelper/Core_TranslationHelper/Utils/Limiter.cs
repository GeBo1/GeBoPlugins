using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using KKAPI.Utilities;
using UnityEngine;

namespace TranslationHelperPlugin.Utils
{
    internal class Limiter
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        private readonly long _limit;
        private long _current;

        internal Limiter(long limit)
        {
            _limit = limit;
        }

        internal bool IsAtLimit()
        {
            return Interlocked.Read(ref _current) >= _limit;
        }

        internal bool IsBelowLimit()
        {
            return !IsAtLimit();
        }
        internal IEnumerator Start()
        {
            if (IsAtLimit())
            {
                yield return null;
                yield return new WaitUntil(IsBelowLimit);
            }
            Interlocked.Increment(ref _current);
        }

        internal IEnumerator End()
        {
            EndImmediately();
            yield break;
        }

        internal void EndImmediately()
        {
            Interlocked.Decrement(ref _current);
        }


    }
}
