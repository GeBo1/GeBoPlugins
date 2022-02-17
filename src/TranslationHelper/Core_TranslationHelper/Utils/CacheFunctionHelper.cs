using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using UnityEngine;

namespace TranslationHelperPlugin.Utils
{
    internal class CacheFunctionHelper
    {
        private readonly HashSet<object> _recentCalls;
        private float _lastCheck;
        private float _staleTime;

        internal CacheFunctionHelper()
        {
            _recentCalls = new HashSet<object>();
            _lastCheck = _staleTime = 0f;

            TranslationHelper.CardTranslationBehaviorChanged += TranslationHelper_BehaviorChanged;
            TranslationHelper.AccelerationBehaviorChanged += TranslationHelper_BehaviorChanged;
        }

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private void TranslationHelper_BehaviorChanged(object sender, EventArgs e)
        {
            Clear();
        }

        private void FreshnessCheck()
        {
            if (Time.fixedUnscaledTime <= _lastCheck) return;
            if (Time.fixedUnscaledTime >= _staleTime && _recentCalls.Count > 0)
            {
                Logger?.DebugLogDebug($"FreshnessCheck clearing: {_recentCalls.Count}");
                _recentCalls.Clear();
            }

            _lastCheck = Time.fixedUnscaledTime;
            _staleTime = _lastCheck + 600f;
        }

        public bool WasCalledRecently(object key)
        {
            FreshnessCheck();
            return _recentCalls.Contains(key);
        }

        public void RecordCall(object key)
        {
            FreshnessCheck();
            _recentCalls.Add(key);
        }

        public void Clear()
        {
            _lastCheck = _staleTime = 0f;
            FreshnessCheck();
        }
    }
}
