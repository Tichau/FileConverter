// <copyright file="FileConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.Collections.Generic;

namespace FileConverter
{
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Xml.Serialization;

    using FileConverter.Annotations;

    [XmlRoot]
    [XmlType]
    public class ConversionPreset : INotifyPropertyChanged, IDataErrorInfo
    {
        private string name;

        private OutputType outputType;

        private List<string> inputTypes;

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

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlAttribute]
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public OutputType OutputType
        {
            get
            {
                return outputType;
            }

            set
            {
                outputType = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public List<string> InputTypes
        {
            get
            {
                return inputTypes;
            }

            set
            {
                inputTypes = value;
                this.OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public ConversionPreset.Setting[] Settings
        {
            get;
            set;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class Setting
        {
        }

        public string this[string columnName]
        {
            get
            {
                return this.Validate(columnName);
            }
        }

        public string Error
        {
            get
            {
                return this.Validate("Name");
            }
        }

        private string Validate(string properyName)
        {
            // Return error message if there is an error, else return empty or null string.
            switch (properyName)
            {
                case "Name":
                    {
                        if (string.IsNullOrEmpty(this.Name))
                        {
                            return "The preset name can't be empty.";
                        }

                        if (this.Name.Contains(";"))
                        {
                            return  "The preset name can't contains the character ';'.";
                        }

                        Application application = Application.Current as Application;
                        if (application?.Settings != null)
                        {
                            int count = application.Settings.ConversionPresets.Count(match => match.name == this.Name);
                            if (count > 1)
                            {
                                return "The preset name is already used.";
                            }
                        }
                    }

                    break;
            }

            return string.Empty;
        }
    }
}
