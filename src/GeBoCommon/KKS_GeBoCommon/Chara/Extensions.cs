using System;
using JetBrains.Annotations;
using KKAPI.MainGame;

namespace GeBoCommon.Chara
{
    public static partial class Extensions
    {
        /// <summary>
        ///     Get the persisting CharaData object that describes this character
        ///     (Heroine or Player depending on sex)
        ///     Returns null if the could not be found. Works only in the main game.
        /// </summary>
        [PublicAPI]
        public static SaveData.CharaData GetCharaData(this ChaControl chaControl)
        {
            if (chaControl == null) throw new ArgumentNullException(nameof(chaControl));
            return chaControl.chaFile.GetCharaData();
        }

        /// <summary>
        ///     Get the persisting CharaData object that describes this character
        ///     (Heroine or Player depending on sex)
        ///     Returns null if the could not be found. Works only in the main game.
        /// </summary>
        [PublicAPI]
        public static SaveData.CharaData GetCharaData(this ChaFileControl chaFile)
        {
            if (chaFile == null) throw new ArgumentNullException(nameof(chaFile));
            switch (chaFile.GetSex())
            {
                case CharacterSex.Female:
                    return chaFile.GetHeroine();

                case CharacterSex.Male:
                    return chaFile.GetPlayer();

                default:
                    return null;
            }
        }

        [PublicAPI]
        public static CharacterSex GetSex(this SaveData.CharaData charaData)
        {
            return (CharacterSex)Manager.Game.CharaDataToSex(charaData);
        }
    }
}
