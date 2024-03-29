﻿namespace TranslationHelperPlugin.MainGame
{
    public partial class Controller
    {
        protected override void OnStartH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            Configuration.OnStartH(this, new HSceneEventArgs(proc, hFlag, vr));
            base.OnStartH(proc, hFlag, vr);
        }

        protected override void OnEndH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            Configuration.OnEndH(this, new HSceneEventArgs(proc, hFlag, vr));
            base.OnEndH(proc, hFlag, vr);
        }
    }
}
