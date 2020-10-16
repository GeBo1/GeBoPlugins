using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TranslationHelperPlugin.Presets.Data
{
    [SerializableAttribute]
    public class NameTranslation
    {
        [XmlArray]
        [XmlArrayItemAttribute("Language")]
        public List<string> TargetLanguages { get; } = new List<string>();

        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string NickName { get; set; }
    }
}
