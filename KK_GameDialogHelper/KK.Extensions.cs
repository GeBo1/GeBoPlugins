using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ActionGame.Communication;

namespace GameDialogHelperPlugin
{
    public static class Extensions
    {
        public static GameDialogHelperController GetGameDialogHelperControler(this ChaControl chaControl)
        {
            if (chaControl != null && chaControl.gameObject != null)
            {
                return chaControl.gameObject.GetComponent<GameDialogHelperController>();
            }
            return null;
        }

        public static void Remember(this ChaControl chaControl, int question, int answer)
        {
            if (chaControl != null)
            {
                var controller = chaControl.GetGameDialogHelperControler();
                if (controller != null)
                {
                    controller.Remember(question, answer);
                }
            }
        }

        public static bool CanRecallQuestion(this ChaControl chaControl, int question)
        {
            if (chaControl != null)
            {
                var controller = chaControl.GetGameDialogHelperControler();
                if (controller != null)
                {
                    return controller.CanRecallQuestion(question);
                }
            }
            return false;
        }

        public static bool CanRecallAnswer(this ChaControl chaControl, int question, int answer)
        {
            if (chaControl != null)
            {
                var controller = chaControl.GetGameDialogHelperControler();
                if (controller != null)
                {
                    return controller.CanRecallAnswer(question, answer);
                }
            }
            return false;
        }

        public static int GetQuestionId(this Info.SelectInfo selectInfo)
        {
            if (selectInfo != null && selectInfo.introduction != null)
            {
                var tmp = selectInfo.introduction.file?.Split('_');
                if (tmp?.Length == 4)
                {
                    try
                    {
                        return int.Parse(tmp[3], NumberStyles.Integer);
                    }
                    catch (OverflowException) { }
                    catch (FormatException) { }
                    catch (ArgumentException) { }
                }
            }
            return -1;
        }

        public static void Remember(this SaveData.Heroine heroine, int question, int answer)
        {
            if (heroine != null && heroine.chaCtrl != null)
            {
                heroine.chaCtrl.Remember(question, answer);
            }
        }

        public static bool CanRecallQuestion(this SaveData.Heroine heroine, int question)
        {
            if (heroine != null && heroine.chaCtrl != null)
            {
                return heroine.chaCtrl.CanRecallQuestion(question);
            }
            return false;
        }

        public static bool CanRecallAnswer(this SaveData.Heroine heroine, int question, int answer)
        {
            if (heroine != null && heroine.chaCtrl != null)
            {
                return heroine.chaCtrl.CanRecallAnswer(question, answer);
            }
            return false;
        }
    }
}
