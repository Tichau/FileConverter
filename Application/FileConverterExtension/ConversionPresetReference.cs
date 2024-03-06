// <copyright file="PresetDefinition.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System.Xml.Serialization;

    [XmlRoot("ConversionPreset")]
    [XmlType("ConversionPreset")]
    public class PresetReference
    {
        private string fullName;

        private PresetReference()
        {
        }

        [XmlAttribute("Name")]
        public string FullName
        {
            get => this.fullName;
            set
            {
                this.fullName = value;

                if (!string.IsNullOrEmpty(this.fullName))
                {
                    string[] folders = this.fullName.Split('/');
                    if (folders.Length > 0)
                    {
                        this.Name = folders[folders.Length - 1];
                        System.Array.Resize(ref folders, folders.Length - 1);
                        this.Folders = folders;
                    }
                }
            }
        }

        [XmlElement]
        public string[] InputTypes
        {
            get;
            set;
        }

        [XmlIgnore]
        public string Name
        {
            get;
            private set;
        }

        [XmlIgnore]
        public string[] Folders
        {
            get;
            private set;
        }
    }
}
