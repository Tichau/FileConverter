// <copyright file="FileConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.Collections.Generic;

namespace FileConverter
{
    using System.Xml.Serialization;

    [XmlRoot]
    [XmlType]
    public class ConversionPreset
    {
        public ConversionPreset()
        {
            this.Name = "New Preset";
        }

        public ConversionPreset(string name, OutputType outputType, string[] inputTypes)
        {
            this.Name = name;
            this.OutputType = outputType;
            this.InputTypes = new List<string>();
            this.InputTypes.AddRange(inputTypes);
        }

        [XmlAttribute]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute]
        public OutputType OutputType
        {
            get;
            set;
        }

        [XmlElement]
        public List<string> InputTypes
        {
            get;
            set;
        }

        [XmlIgnore]
        public ConversionPreset.Setting[] Settings
        {
            get;
            set;
        }

        public class Setting
        {
        }
    }
}
