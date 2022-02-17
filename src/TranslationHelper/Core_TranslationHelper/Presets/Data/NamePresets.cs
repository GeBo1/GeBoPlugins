using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BepInEx.Logging;
using GeBoCommon.Utilities;

namespace TranslationHelperPlugin.Presets.Data
{
    [SerializableAttribute]
    [XmlRoot(Namespace = "", IsNullable = true)]
    public class NamePresets : IList<NamePreset>
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        [XmlElement("NamePreset", IsNullable = true)]
        public List<NamePreset> Entries { get; } = new List<NamePreset>();

        public IEnumerator<NamePreset> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Entries).GetEnumerator();
        }

        public void Add(NamePreset item)
        {
            Entries.Add(item);
        }

        public void Clear()
        {
            Entries.Clear();
        }

        public bool Contains(NamePreset item)
        {
            return Entries.Contains(item);
        }

        public void CopyTo(NamePreset[] array, int arrayIndex)
        {
            Entries.CopyTo(array, arrayIndex);
        }

        public bool Remove(NamePreset item)
        {
            return Entries.Remove(item);
        }

        public int Count => Entries.Count;

        public bool IsReadOnly => ((ICollection<NamePreset>)Entries).IsReadOnly;

        public int IndexOf(NamePreset item)
        {
            return Entries.IndexOf(item);
        }

        public void Insert(int index, NamePreset item)
        {
            Entries.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Entries.RemoveAt(index);
        }

        public NamePreset this[int index]
        {
            get => Entries[index];
            set => Entries[index] = value;
        }


        public static IEnumerable<NamePreset> Load()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var serializer = new XmlSerializer(typeof(NamePresets),
                new XmlRootAttribute($"{nameof(NamePresets)}"));

            XmlSchema schema = null;
            using (var schemaStream =
                   executingAssembly.GetManifestResourceStream(typeof(NamePresets),
                       $"Resources.{nameof(NamePresets)}.xsd")
                  )
            {
                if (schemaStream != null) schema = XmlSchema.Read(schemaStream, null);
            }

            var xmlReaderSettings = new XmlReaderSettings();
            if (schema != null)
            {
                xmlReaderSettings.Schemas.Add(schema);
                xmlReaderSettings.ValidationType = ValidationType.Schema;
            }

            IEnumerable<NamePreset> DeserializeNamePresets(Stream obj, string streamName)
            {
                if (obj == null) yield break;
                Logger.DebugLogDebug($"Loading presets from {streamName}");
                object entryObj;
                try
                {
                    using (var reader = XmlReader.Create(obj, xmlReaderSettings))
                    {
                        entryObj = serializer.Deserialize(reader);
                    }
                }
                catch (InvalidOperationException err)
                {
                    var msg = err.InnerException?.Message ?? err.Message;
                    Logger.LogWarningMessage(
                        $"Unable to load Name Presets from {streamName} (skipping): {msg}");
                    yield break;
                }
                catch (XmlException err)
                {
                    var msg = err.InnerException?.Message ?? err.Message;
                    Logger.LogWarningMessage(
                        $"Unable to load Name Presets from {streamName} (skipping): {msg}");
                    yield break;
                }

                if (!(entryObj is NamePresets entries))
                {
                    Logger.LogWarningMessage(
                        $"Unexpected error loading {streamName} (skipping)");
                    yield break;
                }

                Logger.DebugLogDebug($"{streamName}: {entries.Count} entries");
                foreach (var entry in entries) yield return entry;
            }

            var resourcePrefix = $"{typeof(TranslationHelper).Namespace}.Resources.{nameof(NamePresets)}";
            var resourceNames = executingAssembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(resourcePrefix) && r.EndsWith(".xml"))
                .OrderBy(r => r);


            var resourceLoaded = false;
            foreach (var resourceName in resourceNames)
            {
                using (var stream = executingAssembly.GetManifestResourceStream(resourceName))
                {
                    foreach (var namePreset in DeserializeNamePresets(stream, resourceName)) yield return namePreset;
                    resourceLoaded = true;
                }
            }

            if (!resourceLoaded) Logger.LogWarning("No embedded resources loaded");

            var presetDirs = new[]
            {
                TranslationHelper.ConfigNamePresetDirectory, TranslationHelper.TranslationNamePresetDirectory
            };


            var configFiles = presetDirs.SelectMany(d => Directory
                .GetFiles(d, "*.xml", SearchOption.AllDirectories)
                .Select(n => new FileInfo(n))
                .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase));

            foreach (var configFile in configFiles)
            {
                using (var stream = configFile.OpenRead())
                {
                    foreach (var namePreset in DeserializeNamePresets(stream, configFile.FullName))
                    {
                        yield return namePreset;
                    }
                }
            }
        }
    }
}
