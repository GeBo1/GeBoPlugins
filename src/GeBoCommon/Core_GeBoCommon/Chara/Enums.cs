

using System.Diagnostics.CodeAnalysis;

namespace GeBoCommon.Chara
{

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Game differences")]
    public enum NameType
    {
        Unclassified = 0,
        Given = 1,
        Family = 2
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum CharacterSex
    {
        Unspecified = -1,
        Male = 0,
        Female = 1
    }

}

