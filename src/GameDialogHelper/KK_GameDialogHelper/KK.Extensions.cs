using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ActionGame.Communication;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;

namespace GameDialogHelperPlugin
{
    [PublicAPI]
    public static class Extensions
    {
        #region Player

        public static GameDialogHelperCharaController GetGameDialogHelperController(this SaveData.Player player)
        {
            GameDialogHelperCharaController controller = null;
            player.SafeProc(h => h.chaCtrl.SafeProcObject(cc => controller = cc.GetGameDialogHelperController()));
            return controller;
        }

        #endregion

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

        private static readonly ExpiringSimpleCache<string, Guid> HeroineGuidCache =
            new ExpiringSimpleCache<string, Guid>(
                GenerateHeroineGuid, 600);

        private static Guid GenerateHeroineGuid(string key)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key));
                return new Guid(hash.Skip(Math.Max(0, hash.Length - 16)).ToArray());
            }
        }


        public static GameDialogHelperCharaController GetGameDialogHelperController(this SaveData.Heroine heroine)
        {
            GameDialogHelperCharaController controller = null;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc => controller = cc.GetGameDialogHelperController()));
            return controller;
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

        public static void PersistData(this SaveData.Heroine heroine)
        {
            heroine.SafeProc(h => h.GetGameDialogHelperController().SafeProcObject(dhc => dhc.PersistToCard()));
        }

        public static Guid GetHeroineGuid(this SaveData.Heroine heroine,
            int guidVersion = GameDialogHelper.CurrentHeroineGuidVersion)
        {
            if (guidVersion > GameDialogHelper.MaxHeroineGuidVersion)
                throw new ArgumentOutOfRangeException(nameof(guidVersion), $"Unknown guidVersion ({guidVersion})");

            if (heroine == null || !heroine.charFileInitialized) return Guid.Empty;

            // handle pre-versioned guids
            if (guidVersion < 1) guidVersion = 1;
            
            string guidKey = string.Empty;
            switch (guidVersion)
            {
                case 1:
                    var guidKeyParts = new[]
                        {
                            heroine.FixCharaIDOrPersonality, heroine.schoolClass, heroine.schoolClassIndex
                        }
                        .Select(i => i.ToString("D10")).ToList();
                    guidKeyParts.Add(heroine.Name);
                    guidKey = StringUtils.JoinStrings("/", guidKeyParts.ToArray());
                    break;

                case 2:
                    var guid2KeyParts = new[]
                        {
                            heroine.FixCharaIDOrPersonality, heroine.schoolClass, heroine.schoolClassIndex
                        }
                        .Select(i => i.ToString("D4")).ToList();
                    guid2KeyParts.Add(heroine.Name);
                    guidKey = StringUtils.JoinStrings("/", guid2KeyParts.ToArray());
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(guidVersion),
                        $"Unsupported guidVersion ({guidVersion})");
            }

            return guidKey.IsNullOrEmpty() ? Guid.Empty : HeroineGuidCache.Get(guidKey);
        }

        #endregion Heroine
    }
}
