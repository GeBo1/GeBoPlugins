using System.ComponentModel;
using JetBrains.Annotations;

namespace GameDialogHelperPlugin
{
    public enum PluginMode
    {
        [Description("Relationship Based")]
        RelationshipBased = 1,

        [Description("Advanced Game Logic")]
        Advanced = 2,
        Disabled = int.MinValue
    }

    [PublicAPI]
    public enum RelationshipLevel
    {
        // Always show correct answers
        Anyone = -1,

        // Show correct answers for acquaintances (or higher)
        Acquaintance = 0,

        // Show correct answers for friends (or higher)
        Friend = 1,

        // Show correct answers only if you're dating
        Lover = 2,

        // Disable showing correct answers
        Disabled = int.MinValue
    }

    public enum QuestionType
    {
        Unknown = 0,

        [UsedImplicitly]
        Likes,

        [UsedImplicitly]
        Personality,

        [UsedImplicitly]
        PhysicalAttributes,

        [UsedImplicitly]
        Invitation
    }

    public enum PhysicalAttribute
    {
        None = 0,

        [UsedImplicitly]
        BustSize,

        [UsedImplicitly]
        Height,

        [UsedImplicitly]
        Figure
    }

    public enum LikeTarget
    {
        None = 0,

        [UsedImplicitly]
        Animal,

        [UsedImplicitly]
        BlackCoffee,

        [UsedImplicitly]
        Cook,

        [UsedImplicitly]
        Eat,

        [UsedImplicitly]
        Exercise,

        [UsedImplicitly]
        Fashionable,

        [UsedImplicitly]
        Spicy,

        [UsedImplicitly]
        Study,

        [UsedImplicitly]
        Sweet
    }

    // almost the same as ActionGame.Communication.ResultEnum
    public enum InvitationTarget
    {
        None = 0,

        [UsedImplicitly]
        Chase = 1,

        [UsedImplicitly]
        H = 2,

        [UsedImplicitly]
        Lunch = 3,

        [UsedImplicitly]
        Club = 4,

        [UsedImplicitly]
        GoHome = 5,

        [UsedImplicitly]
        Study = 6,

        [UsedImplicitly]
        Exercise = 7,

        [UsedImplicitly]
        Divorce = 8,

        [UsedImplicitly]
        BecomeLovers,

        [UsedImplicitly]
        Date
    }

    public enum HighlightType
    {
        [UsedImplicitly]
        None = 0,

        [UsedImplicitly]
        Correct = 1,

        [UsedImplicitly]
        Incorrect = 2
    }

    public enum HighlightMode
    {
        [Description("Append Text")]
        AppendText = 0,

        [Description("Change Color")]
        ChangeColor = 1
    }
}
