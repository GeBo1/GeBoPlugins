using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using JetBrains.Annotations;

namespace GameDialogHelperPlugin
{
    public class QuestionInfo
    {
        private static readonly SimpleLazy<QuestionInfo> DefaultLoader = new SimpleLazy<QuestionInfo>(() => new QuestionInfo
        {
            Id = -1, Description = string.Empty, QuestionType = QuestionType.Unknown, InvitationTarget = InvitationTarget.None, 
            LikeTarget = LikeTarget.None, PhysicalAttributeTarget = PhysicalAttribute.None, RelationshipLevel = RelationshipLevel.Anyone
        });

        private static readonly SimpleLazy<Dictionary<int, QuestionInfo>> QuestionInfos =
            new SimpleLazy<Dictionary<int, QuestionInfo>>(() =>
            {
                var result = new Dictionary<int, QuestionInfo>();
                foreach (var qi in LoadQuestions())
                {
                    result[qi.Id] = qi;
                }

                Logger?.LogDebug($"{nameof(QuestionInfo)}: Loaded questions XML: {result.Count}");
                return result;
            });

        private string _description;
        private static ManualLogSource Logger => GameDialogHelper.Logger;

        public static QuestionInfo Default => DefaultLoader.Value;

        public int Id { get; private set; }

        public string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(_description)) return _description;
                
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (QuestionType)
                {
                    case QuestionType.Likes:
                        _description = $"Likes {LikeTarget}";
                        break;

                    case QuestionType.PhysicalAttributes:
                        _description = $"{PhysicalAttributeTarget} preference";
                        break;

                    case QuestionType.Invitation:
                        _description = $"Invitation to {InvitationTarget}";

                        break;

                    default:
                        _description = $"{QuestionType}";
                        break;
                }

                if (RelationshipLevel != RelationshipLevel.Anyone)
                {
                    _description += $" (from {RelationshipLevel})";
                }

                return _description;
            }

            private set => _description = value;
        }

        public QuestionType QuestionType { get; private set; }

        public RelationshipLevel RelationshipLevel { get; private set; }

        public InvitationTarget InvitationTarget { get; private set; }

        public PhysicalAttribute PhysicalAttributeTarget { get; private set; }

        public LikeTarget LikeTarget { get; private set; }

        public override string ToString()
        {
            return $"QuestionInfo({Id}, \"{Description}\")";
        }

        private static IEnumerable<QuestionInfo> LoadQuestions()
        {
            var serializer = new XmlSerializer(typeof(List<QuestionInfo>),
                new XmlRootAttribute($"{nameof(QuestionInfo)}s"));
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(typeof(QuestionInfo), "Resources.QuestionInfos.xml"))
            {
                if (stream == null) return new QuestionInfo[0];

                if (serializer.Deserialize(stream) is List<QuestionInfo> result) return result;
            }

            return new QuestionInfo[0];
        }

        public static QuestionInfo GetById(int questionId)
        {
            return QuestionInfos.Value.TryGetValue(questionId, out var result) ? result : null;
        }
    }

    [UsedImplicitly]
    public class QuestionInfos : List<QuestionInfo> { }
}
