using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ADV;
using KKAPI.MainGame;

namespace GameDressForSuccessPlugin
{
    internal static class Extensions
    {
        public static bool IsNullOrNpc(this SaveData.Heroine heroine)
        {
            return (heroine == null || heroine.FixCharaIDOrPersonality < 0);
        }

        public static bool IsNullOrNpc(this CharaData charaData)
        {
            if (charaData == null || charaData.chaCtrl == null) return true;
            return charaData.chaCtrl.GetHeroine().IsNullOrNpc();
        }
        
        public static int GetCoordinateType(this SaveData.Heroine heroine)
        {
            var result = -1;
            heroine.SafeProc(h => h.chaCtrl.SafeProc(
                cc => cc.chaFile.SafeProc(
                    cf => cf.status.SafeProc(
                        s => result = s.coordinateType))));
            return result;
        }
    }
}
