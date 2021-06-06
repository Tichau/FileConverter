// <copyright file="XmlHelpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    public class XmlHelpers
    {
        public static void LoadFromFile<T>(string root, string path, out T deserializedObject)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            XmlRootAttribute xmlRoot = new XmlRootAttribute
            {
                ElementName = root
            };

            XmlSerializer serializer = new XmlSerializer(typeof(T), xmlRoot);

            using (StreamReader reader = new StreamReader(path))
            {
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                };

                using (XmlReader xmlReader = XmlReader.Create(reader, xmlReaderSettings))
                {
                    deserializedObject = (T)serializer.Deserialize(xmlReader);
                }
            }
        }
        
        public static void SaveToFile<T>(string root, string path, T objectToSerialize)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (objectToSerialize == null)
            {
                throw new ArgumentNullException(nameof(objectToSerialize));
            }
            
            XmlRootAttribute xmlRoot = new XmlRootAttribute
            {
                ElementName = root
            };

            XmlSerializer serializer = new XmlSerializer(typeof(T), xmlRoot);

            using (StreamWriter writer = new StreamWriter(path))
            {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    "
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(writer, xmlWriterSettings))
                {
                    serializer.Serialize(xmlWriter, objectToSerialize);
                }
            }
        }
    }
}