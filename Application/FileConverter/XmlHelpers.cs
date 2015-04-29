using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using FileConverter;

public class XmlHelpers
{
    public static void LoadFromFile<T>(string root, string path, ref List<T> list)
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
                list.AddRange((List<T>)serializer.Deserialize(xmlReader));
            }
        }
        catch (System.Exception exception)
        {
            Diagnostics.Log("The database of type '" + typeof(T) + "' failed to load the asset. The following exception was raised:\n " + exception.Message);
        }
    }

    public static void SaveToFile<T>(string root, string path, List<T> objectsToSerialize)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

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
                serializer.Serialize(xmlWriter, objectsToSerialize);
            }
        }
        catch (System.Exception exception)
        {
            Diagnostics.Log("The database of type '" + typeof(T) + "' failed to load the asset. The following exception was raised:\n " + exception.Message);
        }
    }
}
