// <copyright file="XmlHelpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    public class XmlHelpers
    {
        public static void LoadFromFile<T>(string root, string path, out T deserializedObject)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            XmlRootAttribute xmlRoot = new XmlRootAttribute
            {
                ElementName = root
            };

            XmlSerializer serializer = new XmlSerializer(typeof(T), xmlRoot);

            try
            {
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
            catch (System.Exception)
            {
                throw;
            }

            IXmlSerializable xmlSerializableObject = deserializedObject as IXmlSerializable;
            if (xmlSerializableObject != null)
            {
                xmlSerializableObject.OnDeserializationComplete();
            }
        }
        
        public static void SaveToFile<T>(string root, string path, T objectToSerialize)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (objectToSerialize == null)
            {
                throw new ArgumentNullException("objectToSerialize");
            }
            
            XmlRootAttribute xmlRoot = new XmlRootAttribute
            {
                ElementName = root
            };

            XmlSerializer serializer = new XmlSerializer(typeof(T), xmlRoot);

            try
            {
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
            catch (System.Exception exception)
            {
                Diagnostics.Debug.LogError("Fail to save asset of type '" + typeof(T) + "'. The following exception was raised:\n " + exception.Message);
            }
        }
    }
}