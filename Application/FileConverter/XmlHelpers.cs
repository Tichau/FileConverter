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
        public static void LoadFromFile<T>(string root, string path, ref ICollection<T> collection)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            XmlRootAttribute xmlRoot = new XmlRootAttribute
            {
                ElementName = root
            };

            XmlSerializer serializer = new XmlSerializer(typeof(List<T>), xmlRoot);

            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true, IgnoreComments = true
                    };

                    using (XmlReader xmlReader = XmlReader.Create(reader, xmlReaderSettings))
                    {
                        List<T> elements = (List<T>)serializer.Deserialize(xmlReader);
                        for (int index = 0; index < elements.Count; index++)
                        {
                            T item = elements[index];

                            if (item is IXmlSerializable)
                            {
                                (item as IXmlSerializable).OnDeserializationComplete();
                            }

                            collection.Add(item);
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                Diagnostics.Debug.Log("The database of type '" + typeof(T) + "' failed to load the asset. The following exception was raised:\n " + exception.Message);
            }
        }

        public static void SaveToFile<T>(string root, string path, ICollection<T> objectsToSerialize)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            List<T> list = new List<T>();
            list.AddRange(objectsToSerialize);

            XmlRootAttribute xmlRoot = new XmlRootAttribute
            {
                ElementName = root
            };

            XmlSerializer serializer = new XmlSerializer(typeof(List<T>), xmlRoot);

            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                    {
                        Indent = true, IndentChars = "    "
                    };

                    using (XmlWriter xmlWriter = XmlWriter.Create(writer, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, list);
                    }
                }
            }
            catch (System.Exception exception)
            {
                Diagnostics.Debug.Log("The database of type '" + typeof(T) + "' failed to load the asset. The following exception was raised:\n " + exception.Message);
            }
        }
    }
}