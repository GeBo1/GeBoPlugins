using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Utilities;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio;
using TranslationHelperPlugin.Chara;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

#if AI||HS2
using AIChara;
#endif

namespace TranslationHelperPlugin.Maker
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (StudioAPI.InsideStudio) return;
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            MakerAPI.MakerStartedLoading += MakerStartedLoading;
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
            MakerAPI.MakerExiting += MakerExiting;
            MakerAPI.ReloadCustomInterface += ReloadCustomInterface;


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

        private static void ReloadCustomInterface(object sender, EventArgs e)
        {
            if (e is CharaReloadEventArgs charaReloadEventArgs) MakerCharacterReloaded(sender, charaReloadEventArgs);
        }

        private static void MakerCharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            e?.ReloadedCharacter.SafeProcObject(UpdateUIForChara);
        }

        private static void MakerFinishedLoading(object sender, EventArgs e)
        {
            CharacterApi.CharacterReloaded += MakerCharacterReloaded;
            SetupNameInputFields();

            MakerAPI.GetCharacterControl().SafeProcObject(UpdateUIForChara);
        }


        private static void SetupNameInputFields()
        {
            foreach (var entry in GetNameInputFieldInfos())
            {
                try
                {
                    SetupNameInputField(entry.Key, entry.Value);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    Logger.LogError($"Unable to monitor {entry.Key} InputField for changes: {err}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private static void MakerExiting(object sender, EventArgs e)
        {
            CharacterApi.CharacterReloaded -= MakerCharacterReloaded;
            TranslationHelper.NotifyBehaviorChanged(e);
            // Maker makes heavy use of some pools, clean them up.
            ObjectPool.GlobalClearIdle(10);
        }

        private static void MakerStartedLoading(object sender, RegisterCustomControlsEvent e)
        {
            TranslationHelper.NotifyBehaviorChanged(e);
        }

        private static void UpdateUIForChara(ChaControl chaControl)
        {
            chaControl.SafeProcObject(cc => cc.GetTranslationHelperController().SafeProcObject(
                ctrl => ctrl.StartMonitoredCoroutine(UpdateUICoroutine(ctrl))));
        }

        private static IEnumerator UpdateUICoroutine(Controller controller)
        {
            if (controller == null) yield break;

            controller.TranslateCardNames();
            yield return TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.WaitOnCard(controller.ChaFileControl));
            yield return TranslationHelper.Instance.StartCoroutine(GameSpecificUpdateUICoroutine(controller));
        }

        private static void SetupNameInputField(string name, params string[] inputFieldPath)
        {
            InputField nameField = null;
            var index = GeBoAPI.Instance.ChaFileNameToIndex(name);
            if (index == -1) throw new ArgumentException($"Unknown name: {name}", nameof(name));

            Component top = MakerAPI.GetMakerBase();
            foreach (var fieldName in inputFieldPath)
            {
                if (top == null) break;
                top = top.GetComponentsInChildren<Component>(true).FirstOrDefault(r => r.name == fieldName);
            }


            if (top != null) nameField = top.GetComponent<InputField>();

            if (nameField == null)
            {
                Logger.LogDebug(
                    $"Unable to find {typeof(InputField).FullName} {string.Join("/", inputFieldPath)} (might be NPC)");
                return;
            }

            void Listener(string value)
            {
                var chaCtrl = MakerAPI.GetCharacterControl();
                if (chaCtrl == null) return;
                chaCtrl.GetTranslationHelperController().SafeProcObject(c => c.OnNameChanged(index, value));
            }

            nameField.onValueChanged.AddListener(Listener);
            MakerAPI.MakerExiting += (sender, e) => nameField.onValueChanged.RemoveListener(Listener);
            Logger.LogDebug($"Monitoring {nameField} for changes");
        }
    }
}
