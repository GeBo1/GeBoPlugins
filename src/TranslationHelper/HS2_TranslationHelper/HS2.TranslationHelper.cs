using System;
using BepInEx;
using BepInEx.Configuration;

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
