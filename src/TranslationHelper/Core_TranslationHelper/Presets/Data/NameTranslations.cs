using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TranslationHelperPlugin.Presets.Data
{
    [SerializableAttribute]
    [XmlRoot("Translations", Namespace = "", IsNullable = true)]
    public class NameTranslations : IList<NameTranslation>
    {
        [XmlElement("Translation", IsNullable = true)]
        public List<NameTranslation> Entries { get; } = new List<NameTranslation>();

        public IEnumerator<NameTranslation> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Entries).GetEnumerator();
        }

        public void Add(NameTranslation item)
        {
            Entries.Add(item);
        }

        public void Clear()
        {
            Entries.Clear();
        }

        public bool Contains(NameTranslation item)
        {
            return Entries.Contains(item);
        }

        public void CopyTo(NameTranslation[] array, int arrayIndex)
        {
            Entries.CopyTo(array, arrayIndex);
        }

        public bool Remove(NameTranslation item)
        {
            return Entries.Remove(item);
        }

        public int Count => Entries.Count;

        public bool IsReadOnly => ((ICollection<NameTranslation>)Entries).IsReadOnly;

        public int IndexOf(NameTranslation item)
        {
            return Entries.IndexOf(item);
        }

        public void Insert(int index, NameTranslation item)
        {
            Entries.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Entries.RemoveAt(index);
        }

        public NameTranslation this[int index]
        {
            get => Entries[index];
            set => Entries[index] = value;
        }
    }
}
