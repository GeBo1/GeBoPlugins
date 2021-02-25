using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ActionGame.Communication;
using BepInEx.Logging;
using GameDialogHelperPlugin.Utilities;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using MessagePack;

namespace GameDialogHelperPlugin
{
    [PublicAPI]
    public static class Extensions
    {
        private static ManualLogSource Logger => GameDialogHelper.Logger;

        private static byte[] GetKey(object[] items)
        {
            return items.Select(MessagePackSerializer.Serialize).SelectMany(x => x).ToArray();
        }

        #region SelectInfo

        public static int GetQuestionId(this Info.SelectInfo selectInfo)
        {
            if (selectInfo?.introduction == null) return -1;
            var tmp = selectInfo.introduction.file?.Split('_');
            if (tmp?.Length != 4) return -1;
            try
            {
                return int.Parse(tmp[3], NumberStyles.Integer);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (OverflowException) { }
            catch (FormatException) { }
            catch (ArgumentException) { }
#pragma warning restore CA1031 // Do not catch general exception types

            return -1;
        }

        #endregion SelectInfo

        public static float Average(this List<float> values)
        {
            return values.Count > 0 ? values.Sum() / values.Count : 0f;
        }

        #region CharaData

        public static GameDialogHelperCharaController GetGameDialogHelperController(this SaveData.CharaData charaData)
        {
            GameDialogHelperCharaController controller = null;
            charaData.SafeProc(cd => cd.chaCtrl.SafeProcObject(cc => controller = cc.GetGameDialogHelperController()));
            return controller;
        }

        public static void PersistData(this SaveData.CharaData charaData)
        {
            charaData.SafeProc(cd => cd.GetGameDialogHelperController().SafeProcObject(dhc => dhc.PersistToCard()));
        }

        public static Guid GetCharaGuid(this SaveData.CharaData charaData,
            int guidVersion = PluginDataInfo.CurrentCharaGuidVersion)
        {
            if (guidVersion > PluginDataInfo.MaxCharaGuidVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(guidVersion), $"Unknown guidVersion ({guidVersion})");
            }

            if (guidVersion < PluginDataInfo.MinimumSupportedCharaGuidVersion || charaData == null ||
                !charaData.charFileInitialized) return Guid.Empty;

            var guidKeyBuilder = StringBuilderPool.Get();
            var intFmt = "{0:04}";
            try
            {
                switch (guidVersion)
                {
                    case 5:
                        guidKeyBuilder
                            .AppendFormat(intFmt,
                                charaData is SaveData.Heroine heroine5
                                    ? heroine5.FixCharaIDOrPersonality
                                    : charaData.personality)
                            .Append('/')
                            .AppendFormat(intFmt, charaData.schoolClass)
                            .Append('/')
                            .AppendFormat(intFmt, charaData.schoolClassIndex)
                            .Append('/')
                            .Append(charaData.Name);
                        break;

                    case 6:
                        guidKeyBuilder
                            .AppendFormat(intFmt,
                                charaData is SaveData.Heroine heroine6
                                    ? heroine6.FixCharaIDOrPersonality
                                    : charaData.personality)
                            .Append('/')
                            .AppendFormat(intFmt, charaData.schoolClass)
                            .Append('/')
                            .AppendFormat(intFmt, charaData.schoolClassIndex)
                            .Append('/')
                            .Append(charaData.firstname)
                            .Append('/')
                            .Append(charaData.lastname)
                            .Append('/')
                            .Append(charaData.parameter.sex);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(guidVersion),
                            $"Unsupported guidVersion ({guidVersion})");
                }

                if (guidKeyBuilder.Length == 0) return Guid.Empty;
                var guidKey = guidKeyBuilder.ToString();
                var result = GuidCache.Get(guidKey);
                Logger?.DebugLogDebug(
                    $"{nameof(GetCharaGuid)} (version={guidVersion}): guidKey={guidKey}, result={result}");
                return result;
            }
            finally
            {
                StringBuilderPool.Release(guidKeyBuilder);
            }
        }

        #endregion

        #region Player

        public static GameDialogHelperCharaController GetGameDialogHelperController(this SaveData.Player player)
        {
            GameDialogHelperCharaController controller = null;
            player.SafeProc(h => h.chaCtrl.SafeProcObject(cc => controller = cc.GetGameDialogHelperController()));
            return controller;
        }

        [Obsolete("Use GetCharaGuid")]
        public static Guid GetPlayerGuid(this SaveData.Player player,
            int guidVersion = PluginDataInfo.CurrentSaveGuidVersion)
        {
            return player.GetCharaGuid(guidVersion);
        }

        #endregion

        #region ChaControl

        public static GameDialogHelperCharaController GetGameDialogHelperController(this ChaControl chaControl)
        {
            GameDialogHelperCharaController controller = null;
            chaControl.SafeProcObject(cc =>
                cc.gameObject.SafeProcObject(go => controller = go.GetComponent<GameDialogHelperCharaController>()));
            return controller;
        }

        public static void Remember(this ChaControl chaControl, int question, int answer, bool isCorrect)
        {
            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                controller.Remember(question, answer, isCorrect)));
        }

        public static bool CanRecallQuestion(this ChaControl chaControl, int question)
        {
            var result = false;
            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.CanRecallQuestion(question)));
            return result;
        }

        public static bool CanRecallAnswer(this ChaControl chaControl, int question, int answer)
        {
            var result = false;
            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.CanRecallAnswer(question, answer)));
            return result;
        }

        public static ulong TimesQuestionAnswered(this ChaControl chaControl, int question)
        {
            ulong result = 0;

            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.TimesQuestionAnswered(question)));
            return result;
        }

        public static ulong TimesAnswerSelected(this ChaControl chaControl, int question, int answer)
        {
            ulong result = 0;
            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.TimesAnswerSelected(question, answer)));
            return result;
        }

        public static float CurrentRecallChance(this ChaControl chaControl, int question, int answer)
        {
            var result = 0f;
            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.CurrentRecallChance(question, answer)));
            return result;
        }

        #endregion ChaControl

        #region Heroine

        private static readonly ExpiringSimpleCache<string, Guid> GuidCache =
            new ExpiringSimpleCache<string, Guid>(
                GenerateGuid, 600);

        private static Guid GenerateGuid(string key)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key));
                return new Guid(hash.Skip(Math.Max(0, hash.Length - 16)).ToArray());
            }
        }

        public static void Remember(this SaveData.Heroine heroine, int question, int answer, bool isCorrect)
        {
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc => cc.Remember(question, answer, isCorrect)));
        }

        public static bool CanRecallQuestion(this SaveData.Heroine heroine, int question)
        {
            var result = false;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.CanRecallQuestion(question)));
            return result;
        }

        public static bool CanRecallAnswer(this SaveData.Heroine heroine, int question, int answer)
        {
            var result = false;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.CanRecallAnswer(question, answer)));
            return result;
        }

        public static ulong TimesQuestionAnswered(this SaveData.Heroine heroine, int question)
        {
            ulong result = 0;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.TimesQuestionAnswered(question)));
            return result;
        }

        public static ulong TimesAnswerSelected(this SaveData.Heroine heroine, int question, int answer)
        {
            ulong result = 0;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.TimesAnswerSelected(question, answer)));
            return result;
        }

        public static float CurrentRecallChance(this SaveData.Heroine heroine, int question, int answer)
        {
            var result = 0f;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.CurrentRecallChance(question, answer)));
            return result;
        }

        [Obsolete("Use GetCharaGuid")]
        public static Guid GetHeroineGuid(this SaveData.Heroine heroine,
            int guidVersion = PluginDataInfo.CurrentCharaGuidVersion)
        {
            return heroine.GetCharaGuid(guidVersion);
        }

        #endregion Heroine
    }
}
