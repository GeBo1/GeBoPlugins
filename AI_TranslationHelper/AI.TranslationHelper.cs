using BepInEx;

namespace TranslationHelperPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        internal void GameSpecificAwake()
        {
            AILikeAwake();
        }

        internal void GameSpecificStart()
        {
            AILikeStart();
        }
    }
}
