using BepInEx;
using UnityEngine.SceneManagement;

namespace TranslationHelperPlugin
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationHelper : BaseUnityPlugin
    {
        internal void GameSpecificAwake()
        {
            AILikeAwake();

            // set up for new game dialogs
            SceneManager.sceneLoaded += AI_SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += AI_SceneManager_sceneUnloaded;
        }

        internal void GameSpecificStart()
        {
            AILikeStart();
        }


        private void AI_SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name == "Title_Load") TreatUnknownAsGameMode = true;
        }

        
        private void AI_SceneManager_sceneUnloaded(Scene arg0)
        {
            if (arg0.name == "Title_Load") TreatUnknownAsGameMode = false;
        }

    }
}
