using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GameDialogHelperPlugin
{
    public enum PluginMode : int
    {
        [Description("Highlight answers based on relationships level")]
        RelationshipBased = 1,

        [Description("Uses the players intelligence and past experience to determine if answers are highlighted")]
        Advanced = 2,

        [Description("Disable in game")]
        Disabled = int.MinValue
    }

    public enum RelationshipLevel : int
    {
        [Description("Always show correct answers")]
        Anyone = -1,

        [Description("Show correct answers for acquaintances (or higher)")]
        Acquaintance = 0,

        [Description("Show correct answers for friends (or higher)")]
        Friend = 1,

        [Description("Show correct answers only if you're dating")]
        Lover = 2,

        [Description("Disable showing correct answers")]
        Disabled = int.MinValue
    }

    public enum QuestionType : int
    {
        Unknown = 0,
        Likes,
        Personality,
        PhysicalAttributes,
        Invitation
    }

    public enum PhysicalAttribute : int
    {
        None = 0,
        BustSize,
        Height,
        Figure
    }

    public enum LikeTarget : int
    {
        None = 0,
        Animal,
        BlackCoffee,
        Cook,
        Eat,
        Exercise,
        Fashionable,
        Spicy,
        Study,
        Sweet
    }

    // almost the same as ActionGame.Communication.ResultEnum
    public enum InvitationTarget : int
    {
        None = 0,
        Chase = 1,
        H = 2,
        Lunch = 3,
        Club = 4,
        GoHome = 5,
        Study = 6,
        Exercise = 7,
        Divorce = 8,
        BecomeLovers,
        Date
    }
}
