using BepInEx.Logging;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio;
using UniRx;

namespace TranslationHelperPlugin.Maker
{
    internal static partial class Configuration
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (StudioAPI.InsideStudio) return;
            Logger.LogInfo($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            MakerAPI.RegisterCustomSubCategories += (sender, e) =>
            {
                var sidebarToggle = e.AddSidebarControl(new SidebarToggle("Save with translated names",
                    TranslationHelper.MakerSaveWithTranslatedNames.Value, TranslationHelper.Instance));

                sidebarToggle.ValueChanged.Subscribe(b =>
                    TranslationHelper.MakerSaveWithTranslatedNames.Value = b);

                MakerAPI.MakerExiting += (s, e2) => sidebarToggle = null;
            };

            GameSpecificSetup(harmony);
        }
    }
}
