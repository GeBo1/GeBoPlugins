using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using GeBoCommon;
using JetBrains.Annotations;

namespace TranslationHelperPlugin
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInProcess(Constants.MainGameProcessNameVR)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        public static ConfigEntry<bool> KKS_GivenNameFirst { get; private set; }


        // ReSharper disable once MemberCanBeMadeStatic.Global
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        internal void GameSpecificAwake()
        {
            SplitNamesBeforeTranslate = false;
        }

        internal void GameSpecificStart()
        {
            SplitNamesBeforeTranslate = false;

            KKS_GivenNameFirst = Config.Bind("Translate Card Name Options", "Show given name first",
                false, "Reverses the order of names to be Given Family instead of Family Given");

            KKS_GivenNameFirst.SettingChanged += GivenNameFirstChanged;
            GivenNameFirstChanged(this, new SettingChangedEventArgs(KKS_GivenNameFirst));
        }

        private void GivenNameFirstChanged(object sender, EventArgs e)
        {
            ShowGivenNameFirst = KKS_GivenNameFirst.Value;
            OnCardTranslationBehaviorChanged(e);
        }

        [UsedImplicitly]
        internal static string ProcessFullnameString(string input)
        {
            if (!ShowGivenNameFirst) return input;
            if (string.IsNullOrEmpty(input)) return input;
            var parts = input.Split();
            return parts.Length != 2 ? input : string.Join(SpaceJoiner, parts.Reverse().ToArray());
        }
    }
}
