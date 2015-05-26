using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Xml;
using System.Xml.Serialization;

using FileConverter;

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
            using (XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true }))
            {
                List<T> elements = (List<T>)serializer.Deserialize(xmlReader);
                foreach (T element in elements)
                {
                    collection.Add(element);
                }
            }
        }
        catch (System.Exception exception)
        {
            Diagnostics.Log("The database of type '" + typeof(T) + "' failed to load the asset. The following exception was raised:\n " + exception.Message);
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
            using (XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, IndentChars = "    " }))
            {
                serializer.Serialize(xmlWriter, list);
            }
        }
        catch (System.Exception exception)
        {
            Diagnostics.Log("The database of type '" + typeof(T) + "' failed to load the asset. The following exception was raised:\n " + exception.Message);
        }
    }
}
