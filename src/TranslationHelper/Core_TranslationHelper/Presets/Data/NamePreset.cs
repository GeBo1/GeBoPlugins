using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using BepInEx.Logging;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Presets.Data
{
    [Serializable]
    [XmlRoot(Namespace = "", IsNullable = true)]
    public class NamePreset
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        public CharacterSex Sex { get; set; } = CharacterSex.Unspecified;

        [XmlArray]
        [XmlArrayItem(ElementName = "Name")]
        public List<string> GivenNames { get; } = new List<string>();

        [XmlArray]
        [XmlArrayItem(ElementName = "Name")]
        public List<string> FamilyNames { get; } = new List<string>();

        [XmlArray]
        [XmlArrayItem(ElementName = "Name")]
        public List<string> NickNames { get; } = new List<string>();

        [XmlArrayItem(ElementName = "Translation")]
        public List<NameTranslation> Translations { get; } = new List<NameTranslation>();

        public string Notes { get; set; }

        internal NameTranslation GetTranslation(string locale)
        {
            NameTranslation result = null;
            foreach (var translation in Translations)
            {
                if (translation.TargetLanguages.Count == 0)
                {
                    if (result == null) result = translation;
                    continue;
                }

                if (!translation.TargetLanguages.Contains(locale, StringComparer.OrdinalIgnoreCase)) continue;
                return translation;
            }

            return result;
        }
    }
}
