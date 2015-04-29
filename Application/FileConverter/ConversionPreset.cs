using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [System.Xml.Serialization.XmlAttribute]
        public string Name
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlAttribute]
        public OutputType OutputType
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlElement]
        public List<string> InputTypes
        {
            get;
            set;
        }

        [System.Xml.Serialization.XmlIgnore]
        public Setting[] Settings
        {
            get;
            set;
        }

        public class Setting
        {
            
        }
    }
}
