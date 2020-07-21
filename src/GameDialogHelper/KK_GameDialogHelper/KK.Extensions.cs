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
            return chaControl?.gameObject?.GetComponent<GameDialogHelperController>();
        }

        public static void Remember(this ChaControl chaControl, int question, int answer)
        {
            chaControl?.GetGameDialogHelperController()?.Remember(question, answer);
        }

        public static bool CanRecallQuestion(this ChaControl chaControl, int question)
        {
            return chaControl?.GetGameDialogHelperController()?.CanRecallQuestion(question) ?? false;
        }

        public static bool CanRecallAnswer(this ChaControl chaControl, int question, int answer)
        {
            return chaControl?.GetGameDialogHelperController()?.CanRecallAnswer(question, answer) ?? false;
        }

        public static int TimesQuestionAnswered(this ChaControl chaControl, int question)
        {
            return chaControl?.GetGameDialogHelperController()?.TimesQuestionAnswered(question) ?? 0;
        }

        public static int TimesAnswerSelected(this ChaControl chaControl, int question, int answer)
        {
            return chaControl?.GetGameDialogHelperController()?.TimesAnswerSelected(question, answer) ?? 0;
        }

        #endregion ChaControl

        #region Heroine
        public static void Remember(this SaveData.Heroine heroine, int question, int answer)
        {
            heroine?.chaCtrl?.Remember(question, answer);
        }

        public static bool CanRecallQuestion(this SaveData.Heroine heroine, int question)
        {
            return heroine?.chaCtrl?.CanRecallQuestion(question) ?? false;
        }

        public static bool CanRecallAnswer(this SaveData.Heroine heroine, int question, int answer)
        {
            return heroine?.chaCtrl?.CanRecallAnswer(question, answer) ?? false;
        }

        public static int TimesQuestionAnswered(this SaveData.Heroine heroine, int question)
        {
            return heroine?.chaCtrl?.TimesQuestionAnswered(question) ?? 0;
        }

        public static int TimesAnswerSelected(this SaveData.Heroine heroine, int question, int answer)
        {
            return heroine?.chaCtrl?.TimesAnswerSelected(question, answer) ?? 0;
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
