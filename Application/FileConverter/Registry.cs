// <copyright file="Registry.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    public class Registry : IDisposable
    {
        private static Registry instance;

        private Dictionary<string, string> registryEntries = new Dictionary<string, string>();

        ~Registry()
        {
            this.Dispose();
            Registry.instance = null;
        }

        [XmlElement("Entry")]
        public Entry[] SerializableEntries
        {
            get
            {
                Entry[] entries = new Entry[this.registryEntries.Count];
                int index = 0;
                foreach (KeyValuePair<string, string> kvp in this.registryEntries)
                {
                    if (kvp.Value == null)
                    {
                        continue;
                    }

                    entries[index] = new Entry(kvp.Key, kvp.Value);
                    index++;
                }

                return entries;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                this.registryEntries.Clear();
                for (int index = 0; index < value.Length; index++)
                {
                    Entry entry = value[index];
                    if (entry.Key == null)
                    {
                        continue;
                    }

                    if (this.registryEntries.ContainsKey(entry.Key))
                    {
                        Diagnostics.Debug.Log("Ignore registry entry {0}.", entry.Key);
                        continue;
                    }

                    this.registryEntries.Add(entry.Key, entry.Value);
                }
            }
        }

        private static Registry Instance
        {
            get
            {
                if (Registry.instance == null)
                {
                    Registry.Load();
                }

                return Registry.instance;
            }
        }

        private static string GetUserRegistryFilePath
        {
            get
            {
                string path = FileConverterExtension.PathHelpers.GetUserDataFolderPath;
                path = Path.Combine(path, "Registry.xml");
                return path;
            }
        }

        public static T GetValue<T>(string key, T defaultValue = default(T))
        {
            Registry registry = Registry.Instance;

            if (!registry.registryEntries.TryGetValue(key, out string stringValue))
            {
                return defaultValue;
            }

            try
            {
                object value = System.Convert.ChangeType(stringValue, typeof(T));
                return (T)value;
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.LogError("Can't convert registry value: {0}.", exception.Message);
            }

            return defaultValue;
        }

        public static void SetValue<T>(string key, T value)
        {
            Registry registry = Registry.Instance;

            if (!registry.registryEntries.ContainsKey(key))
            {
                registry.registryEntries.Add(key, null);
            }

            try
            {
                string stringValue = (string)System.Convert.ChangeType(value, typeof(string));
                registry.registryEntries[key] = stringValue;
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.LogError("Can't convert registry value: {0}.", exception.Message);
            }
        }

        public void Dispose()
        {
            // SAVE
            string registryFilePath = Registry.GetUserRegistryFilePath;

            try
            {
                XmlHelpers.SaveToFile("Registry", registryFilePath, this);
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.LogError("Fail to save registry. {0}", exception.Message);
            }
        }
        
        private static void Load()
        {
            string registryFilePath = Registry.GetUserRegistryFilePath;
            if (!File.Exists(registryFilePath))
            {
                Registry.instance = new Registry();
                return;
            }

            try
            {
                Registry.instance = null;
                XmlHelpers.LoadFromFile<Registry>("Registry", registryFilePath, out Registry.instance);
            }
            catch (Exception exception)
            {
                Registry.instance = new Registry();
                Diagnostics.Debug.LogError("Fail to load registry. {0}", exception.Message);
                while (exception.InnerException != null)
                {
                    exception = exception.InnerException;
                    Diagnostics.Debug.LogError("Inner exception: {0}", exception.Message);
                }
            }
        }

        [XmlRoot("Entry")]
        public struct Entry
        {
            public Entry(string key, string value)
            {
                this.Key = key;
                this.Value = value;
            }

            [XmlAttribute]
            public string Key
            {
                get;
                set;
            }

            [XmlText]
            public string Value
            {
                get;
                set;
            }
        }

        public static class Keys
        {
            public static readonly string LastUpdateCheckDate = "LastUpdateCheckDate";
            public static readonly string ImportInitialFolder = "ImportInitialFolder";
        }
    }
}
