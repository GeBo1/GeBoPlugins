﻿using System;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using JetBrains.Annotations;

namespace TranslationHelperPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(StudioCharaSortGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        private const string StudioCharaSortGUID = "kky.kk.studiocharasort";
        public static ConfigEntry<bool> KK_GivenNameFirst { get; private set; }

        internal static void GameSpecificAwake()
        {
            SplitNamesBeforeTranslate = false;
        }

        internal void GameSpecificStart()
        {
            SplitNamesBeforeTranslate = false;

            KK_GivenNameFirst = Config.Bind("Translate Card Name Options", "Show given name first",
                false, "Reverses the order of names to be Given Family instead of Family Given");

            KK_GivenNameFirst.SettingChanged += GivenNameFirstChanged;
            GivenNameFirstChanged(this, new SettingChangedEventArgs(KK_GivenNameFirst));
        }

        private void GivenNameFirstChanged(object sender, EventArgs e)
        {
            ShowGivenNameFirst = KK_GivenNameFirst.Value;
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

        private bool GetEnableCardListTranslationCachingDefault()
        {
            if (Chainloader.PluginInfos.TryGetValue(StudioCharaSortGUID, out var pluginInfo))
            {
                Logger.LogWarning(
                    $"Detected {pluginInfo.Metadata.Name} v{pluginInfo.Metadata.Version}: If you are seeing duplicate character names disable {PluginName} / Advanced / {CardListTranslationCachingConfigName}");
                if (pluginInfo.Metadata.Version >= new Version("1.0.2.0"))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
