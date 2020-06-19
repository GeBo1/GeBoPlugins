using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChaCustom;
using GeBoCommon;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace TranslationHelperPlugin.Maker
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            MakerAPI.MakerExiting += MakerAPI_MakerExiting;
        }

        private static void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
        {
            var inputFields = new List<string[]>
            {
                new[] {"firstname", "CharactorTop", "InputName", "InpFirstName"},
                new[] {"lastname", "CharactorTop", "InputName", "InpLastName"},
                new[] {"nickname", "CharactorTop", "InputNickName", "InpNickName"}
            };

            foreach (var entry in inputFields)
            {
                try
                {
                    SetupNameInputField(entry[0], entry.Skip(1).ToArray());
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    Logger.LogError($"Unable to monitor {entry[0]} InputField for changes: {err}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;

            MakerAPI.GetCharacterControl().SafeProcObject(UpdateUIForChara);
        }

        private static void CharacterApi_CharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            e?.ReloadedCharacter.SafeProcObject(UpdateUIForChara);
        }

        private static void MakerAPI_MakerExiting(object sender, EventArgs e)
        {
            CharacterApi.CharacterReloaded -= CharacterApi_CharacterReloaded;
        }


        private static void UpdateUIForChara(ChaControl chaControl)
        {
            chaControl.SafeProcObject(cc => cc.GetTranslationHelperController().SafeProcObject(
                ctrl => ctrl.StartMonitoredCoroutine(UpdateUICoroutine(ctrl))));
        }

        private static IEnumerator UpdateUICoroutine(Controller controller)
        {
            if (controller == null) yield break;

            yield return TranslationHelper.WaitOnCard(controller.ChaFileControl);

            var makerBase = MakerAPI.GetMakerBase();
            if (makerBase == null) yield break;

            makerBase.GetComponentInChildren<CvsChara>().SafeProcObject(o => o.UpdateCustomUI());
            makerBase.GetComponentInChildren<CvsCharaEx>().SafeProcObject(o => o.UpdateCustomUI());
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
                top = top.GetComponentsInChildren<Component>()
                    .FirstOrDefault(r => r.name == fieldName);
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
