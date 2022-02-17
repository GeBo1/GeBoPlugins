using System;
using BepInEx.Configuration;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin
{
    public partial class TranslationHelper
    {
        public static ConfigEntry<bool> AILike_SplitNamesBeforeTranslate { get; private set; }


        internal void AILikeAwake()
        {
            AILike_SplitNamesBeforeTranslate = Config.Bind("Translate Card Name Options",
                "Split Names Before Translate",
                true, "Split on space and translate names by sections");
            AILike_SplitNamesBeforeTranslate.SettingChanged += AILike_SplitNamesBeforeTranslate_SettingChanged;
            SplitNamesBeforeTranslate = AILike_SplitNamesBeforeTranslate.Value;
        }

        internal void AILikeStart()
        {
            Assert.IsNotNull(this);
        }

        private static void AILike_SplitNamesBeforeTranslate_SettingChanged(object sender, EventArgs e)
        {
            SplitNamesBeforeTranslate = AILike_SplitNamesBeforeTranslate.Value;
        }
    }
}
