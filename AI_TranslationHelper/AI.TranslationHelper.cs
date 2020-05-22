using BepInEx;
using BepInEx.Configuration;

namespace TranslationHelperPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        #region ConfigMgr

        public static ConfigEntry<bool> AI_SplitNamesBeforeTranslate { get; private set; }

        #endregion ConfigMgr

        internal void GameSpecificAwake()
        {
            AI_SplitNamesBeforeTranslate = Config.Bind("Translate Card Name Options", "Split Names Before Translate", false, "Split on space and translate names by sections");
            AI_SplitNamesBeforeTranslate.SettingChanged += AI_SplitNamesBeforeTranslate_SettingChanged;
            SplitNamesBeforeTranslate = AI_SplitNamesBeforeTranslate.Value;
        }

        internal void GameSpecificStart()
        {
        }

        private void AI_SplitNamesBeforeTranslate_SettingChanged(object sender, System.EventArgs e)
        {
            SplitNamesBeforeTranslate = AI_SplitNamesBeforeTranslate.Value;
        }
    }
}
