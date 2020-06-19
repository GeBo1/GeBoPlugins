using System.Collections;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon.Chara;
using KKAPI.Chara;
using KKAPI.Studio;
using Studio;
using TranslationHelperPlugin.Chara;
using IllusionStudio = Studio.Studio;

#if AI || HS2
using AIChara;
#endif


namespace TranslationHelperPlugin.Studio
{
    internal static partial class Configuration
    {
        internal const string GUID = TranslationHelper.GUID + ".translationscope";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (!StudioAPI.InsideStudio) return;
            Logger.LogInfo($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;

            GameSpecificSetup(harmony);
        }

        private static void CharacterApi_CharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            e?.ReloadedCharacter.SafeProcObject(UpdateTreeForChar);
        }

        private static void UpdateTreeForChar(ChaControl chaControl)
        {
            //Logger.LogDebug($"UpdateTreeForChar {chaControl}");
            chaControl.SafeProcObject(cc => cc.GetTranslationHelperController().SafeProcObject(
                ctrl => ctrl.StartMonitoredCoroutine(UpdateTreeCoroutine(ctrl))));
        }

        private static IEnumerator UpdateTreeCoroutine(Controller controller)
        {
            //Logger.LogDebug($"UpdateTreeCoroutine {controller}");
            if (controller == null) yield break;
            yield return null;
            var match = Singleton<IllusionStudio>.Instance.dicInfo
                .Where(e => e.Value is OCIChar ociChar && ociChar.charInfo.chaFile == controller.ChaFileControl)
                .Select(e => e.Key).FirstOrDefault();
            if (match == null) yield break;
            yield return TranslationHelper.WaitOnCard(controller.ChaFileControl);
            match.textName = controller.ChaFileControl.GetFullName();
            //Logger.LogDebug($"UpdateTreeCoroutine {controller} DONE");
        }
    }
}
