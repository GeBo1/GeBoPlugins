using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;

#if HS2 || AI
#endif

namespace TranslationHelperPlugin.Chara
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public partial class Controller : CharaCustomFunctionController
    {
        private readonly List<IEnumerator> _monitoredCoroutines = new List<IEnumerator>();

        private readonly SimpleLazy<string[]> _originalNames =
            new SimpleLazy<string[]>(() => new string[GeBoAPI.Instance.ChaFileNameCount]);

        private readonly SimpleLazy<string[]> _translatedNames =
            new SimpleLazy<string[]>(() => new string[GeBoAPI.Instance.ChaFileNameCount]);

        private string _fullPath;
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private static bool RestoreNamesOnSave =>
            !MakerAPI.InsideAndLoaded || !TranslationHelper.MakerSaveWithTranslatedNames.Value;

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
            Justification = "Workaround to avoid constructor in controllers")]
        public string[] TranslatedNames => _translatedNames.Value;

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
            Justification = "Workaround to avoid constructor in controllers")]
        public string[] OriginalNames => _originalNames.Value;

        // ReSharper disable once MergeConditionalExpression
        private string RegistrationID => ChaFileControl != null ? ChaFileControl.GetRegistrationID() : null;
        public bool IsTranslated { get; private set; }
        public bool TranslationInProgress { get; private set; }

        public string FullPath
        {
            get
            {
                if (!_fullPath.IsNullOrEmpty()) return _fullPath;
                if (Configuration.TryGetCharaFileControlPath(ChaFileControl, out var value))
                {
                    _fullPath = PathUtils.NormalizePath(value);
                }

                return _fullPath;
            }
            internal set
            {
                try
                {
                    _fullPath = PathUtils.NormalizePath(value);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    Logger.LogDebug($"FullPath: Unable to normalize '{value}'");
                    // not trackable in main game
                    _fullPath = null;
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        protected override void OnDestroy()
        {
            StopMonitoredCoroutines();
            if (!TranslationHelper.IsShuttingDown)
            {
                UnregisterReplacements();
                RestoreCardNames();
            }

            base.OnDestroy();
        }

        internal Coroutine StartMonitoredCoroutine(IEnumerator routine)
        {
            _monitoredCoroutines.Add(routine);
            return StartCoroutine(routine.AppendCo(() => { _monitoredCoroutines.Remove(routine); }));
        }

        internal void SetTranslatedName(int index, string value)
        {
            IsTranslated = IsTranslated || OriginalNames[index] != value;
            TranslatedNames[index] = value;
            ChaFileControl.SetName(index, value);
        }

        internal void OnNameChanged(int index, string value)
        {
            TranslatedNames[index] = null;
            OriginalNames[index] = value;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            //Logger?.LogDebug($"Controller.OnCardBeingSaved: {RegistrationID}");
            if (RestoreNamesOnSave) RestoreCardNames();
            SetExtendedData(null);
        }

        internal void OnCardSaveComplete(GameMode gameMode)
        {
            //Logger?.LogDebug($"Controller.OnCardSaveComplete: {RegistrationID}");
            if (!RestoreNamesOnSave) return;
            IsTranslated = false;
            for (var i = 0; i < GeBoAPI.Instance.ChaFileNameCount; i++)
            {
                OriginalNames[i] = TranslatedNames[i] = null;
            }

            TranslateCardNames();
            var _ = gameMode;
        }

        private void DoReload(bool cardFullyLoaded = true)
        {
            IsTranslated = false;
            for (var i = 0; i < GeBoAPI.Instance.ChaFileNameCount; i++)
            {
                OriginalNames[i] = TranslatedNames[i] = null;
            }

            TranslateCardNames(cardFullyLoaded);
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            DoReload();
        }

        internal void OnAlternateReload()
        {
            DoReload();
        }

        private void StopMonitoredCoroutines()
        {
            var toStop = _monitoredCoroutines.ToList();
            toStop.Reverse();

            foreach (var routine in toStop)
            {
                //Logger.LogWarning($"Stopping {routine}");
                try
                {
                    StopCoroutine(routine);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    Logger.LogException(err, this, $"error stopping {routine}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            _monitoredCoroutines.Clear();
        }

        private void RestoreCardNames()
        {
            Logger?.DebugLogDebug($"Controller.RestoreCardNames: {RegistrationID}");
            if (!IsTranslated || TranslationHelper.IsShuttingDown || GeBoAPI.Instance == null) return;
            IsTranslated = false;
            for (var i = 0; i < GeBoAPI.Instance.ChaFileNames.Count; i++)
            {
                // only restore value if it hasn't been changed
                if (OriginalNames[i] == null) continue;

                var current = ChaFileControl.GetName(i);
                if (current == TranslatedNames[i])
                {
                    ChaFileControl.SetName(i, OriginalNames[i]);
                }
                else
                {
                    // value has changed, so dump the translation and update original
                    TranslatedNames[i] = null;
                    OriginalNames[i] = current;
                }
            }
        }

        public void TranslateCardNames(bool cardFullyLoaded = true)
        {
            Logger?.DebugLogDebug($"Controller.TranslateCardNames: {RegistrationID} {IsTranslated}");
            if (TranslationHelper.Instance.CurrentCardLoadTranslationMode == CardLoadTranslationMode.Disabled) return;
            if (!IsTranslated)
            {
                foreach (var entry in ChaFileControl.EnumerateNames())
                {
                    OriginalNames[entry.Key] = entry.Value;
                }
            }

            TranslationInProgress = true;

            StartMonitoredCoroutine(TranslationHelper.CardNameManager.TranslateCardNames(ChaFileControl));

            StartMonitoredCoroutine(WaitOnTranslations().AppendCo(
                () => OnTranslationComplete(cardFullyLoaded)));
        }

        public IEnumerator WaitOnTranslations()
        {
            return TranslationHelper.CardNameManager.WaitOnCard(ChaFileControl);
        }

        public void RegisterReplacements(bool alreadyTranslated = false)
        {
            //Logger?.DebugLogDebug($"Controller.RegisterReplacements: {RegistrationID}");
            if (!TranslationHelper.RegisterActiveCharacters.Value ||
                !TranslationHelper.RegistrationGameModes.Contains(TranslationHelper.Instance.CurrentGameMode))
            {
                return;
            }

            StartMonitoredCoroutine(
                TranslationHelper.Instance.RegisterReplacementsWrapper(ChaFileControl, alreadyTranslated));
        }

        public void UnregisterReplacements()
        {
            //Logger?.DebugLogDebug($"Controller.UnregisterReplacements: {RegistrationID}");
            if (TranslationHelper.IsShuttingDown) return;
            TranslationHelper.Instance.SafeProc(i => i.UnregisterReplacements(ChaFileControl).RunImmediately());
        }

        public void OnTranslationComplete(bool cardFullyLoaded = false)
        {
            Logger?.DebugLogDebug($"Controller.OnTranslationComplete: {RegistrationID}");
            TranslationInProgress = false;
            if (!cardFullyLoaded) return;

            RegisterReplacements(true);
        }


        internal void ApplyTranslations()
        {
            if (!IsTranslated) return;
            for (var i = 0; i < TranslatedNames.Length; i++)
            {
                var translatedName = TranslatedNames[i];
                if (string.IsNullOrEmpty(translatedName)) continue;
                ChaFileControl.SetName(i, translatedName);
            }
        }

        [UsedImplicitly]
        public string GetFormattedOriginalName()
        {
#if KK
            if (!TranslationHelper.ShowGivenNameFirst) return GetOriginalFullName();
            var givenIdx = GeBoAPI.Instance.ChaFileNameToIndex("firstname");
            var givenName = IsTranslated && !string.IsNullOrEmpty(OriginalNames[givenIdx])
                ? OriginalNames[givenIdx]
                : ChaFileControl.GetName(givenIdx);

            var familyIdx = GeBoAPI.Instance.ChaFileNameToIndex("lastname");
            var familyName = IsTranslated && !string.IsNullOrEmpty(OriginalNames[familyIdx])
                ? OriginalNames[familyIdx]
                : ChaFileControl.GetName(familyIdx);
            return string.Concat(givenName, " ", familyName);
#else
            return GetOriginalFullName();
#endif
        }


        public string GetOriginalFullName()
        {
#if KK
            var givenIdx = GeBoAPI.Instance.ChaFileNameToIndex("firstname");
            var givenName = IsTranslated && !string.IsNullOrEmpty(OriginalNames[givenIdx])
                ? OriginalNames[givenIdx]
                : ChaFileControl.GetName(givenIdx);

            var familyIdx = GeBoAPI.Instance.ChaFileNameToIndex("lastname");
            var familyName = IsTranslated && !string.IsNullOrEmpty(OriginalNames[familyIdx])
                ? OriginalNames[familyIdx]
                : ChaFileControl.GetName(familyIdx);
            return string.Concat(familyName, " ", givenName);
#else
            if (!IsTranslated) return ChaFileControl.GetFullName();
            var idx = GeBoAPI.Instance.ChaFileNameToIndex("fullname");
            return string.IsNullOrEmpty(OriginalNames[idx]) ? ChaFileControl.GetFullName() : OriginalNames[idx];
#endif
        }
    }
}
