#if AI||KKS||KK
using System;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using KKAPI.MainGame;

namespace TranslationHelperPlugin.MainGame
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public partial class Controller : GameCustomFunctionController
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            TranslationHelper.RegistrationManager.Cleanup();
            Configuration.OnGameLoad(this, args);
            base.OnGameLoad(args);
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            Configuration.OnGameSave(this, args);
            base.OnGameSave(args);
        }

        protected override void OnNewGame()
        {
            Configuration.OnNewGame(this, EventArgs.Empty);
            base.OnNewGame();
        }
    }
}
#endif
