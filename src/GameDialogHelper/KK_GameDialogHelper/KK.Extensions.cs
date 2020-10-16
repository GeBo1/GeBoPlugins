using System;
using System.Globalization;
using ActionGame.Communication;

namespace GameDialogHelperPlugin
{
    public static class Extensions
    {
        #region ChaControl
        public static GameDialogHelperController GetGameDialogHelperController(this ChaControl chaControl)
        {
            GameDialogHelperController controller = null;
            chaControl.SafeProcObject(cc =>
                cc.gameObject.SafeProcObject(go => controller = go.GetComponent<GameDialogHelperController>()));
            return controller;
        }

        public static void Remember(this ChaControl chaControl, int question, int answer)
        {
            chaControl.SafeProcObject(cc=>cc.GetGameDialogHelperController().SafeProcObject(controller=>
                    controller.Remember(question, answer)));
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

        public static int TimesQuestionAnswered(this ChaControl chaControl, int question)
        {
            var result = 0;

            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.TimesQuestionAnswered(question)));
            return result;
        }

        public static int TimesAnswerSelected(this ChaControl chaControl, int question, int answer)
        {
            var result = 0;
            chaControl.SafeProcObject(cc => cc.GetGameDialogHelperController().SafeProcObject(controller =>
                result = controller.TimesAnswerSelected(question, answer)));
            return result;
        }

        #endregion ChaControl

        #region Heroine
        public static void Remember(this SaveData.Heroine heroine, int question, int answer)
        {
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc => cc.Remember(question, answer)));
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

        public static int TimesQuestionAnswered(this SaveData.Heroine heroine, int question)
        {
            var result = 0;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.TimesQuestionAnswered(question)));
            return result;
        }

        public static int TimesAnswerSelected(this SaveData.Heroine heroine, int question, int answer)
        {
            var result = 0;
            heroine.SafeProc(h => h.chaCtrl.SafeProcObject(cc =>
                result = cc.TimesAnswerSelected(question, answer)));
            return result;
        }

        #endregion Heroine

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

    }
}
