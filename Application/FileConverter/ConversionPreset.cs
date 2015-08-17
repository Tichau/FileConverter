// <copyright file="FileConverter.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
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
        private Dictionary<string, string> settings = new Dictionary<string, string>(); 

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

        [XmlElement]
        public ConversionSetting[] Settings
        {
            get
            {
                int index = 0;
                ConversionSetting[] settings = new ConversionSetting[this.settings.Count];
                foreach (KeyValuePair<string, string> keyValuePair in this.settings)
                {
                    settings[index] = new ConversionSetting(keyValuePair);
                    index++;
                }

                return settings;
            }

            set
            {
                this.settings.Clear();
                if (value != null)
                {
                    for (int index = 0; index < value.Length; index++)
                    {
                        this.SetSettingsValue(value[index].Key, value[index].Value);
                    }
                }

                this.OnPropertyChanged();
            }
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

        public void SetSettingsValue(string settingsKey, string value)
        {
            if (string.IsNullOrEmpty(settingsKey))
            {
                throw new ArgumentNullException("key");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("value");
            }

            if (!this.settings.ContainsKey(settingsKey))
            {
                this.settings.Add(settingsKey, value);
            }

            this.settings[settingsKey] = value;

            this.OnPropertyChanged("Settings");
        }

        public string GetSettingsValue(string settingsKey)
        {
            if (string.IsNullOrEmpty(settingsKey))
            {
                throw new ArgumentNullException("key");
            }

            if (this.settings.ContainsKey(settingsKey))
            {
                return this.settings[settingsKey];
            }

            return this.GetDefaultValue(settingsKey);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string GetDefaultValue(string settingsKey)
        {
            if (this.OutputType == OutputType.Mp3)
            {
                switch (settingsKey)
                {
                    case "Encoding":
                        return "VBR";

                    case "Bitrate":
                        return "190";
                }
            }

            return null;
        }

        private string Validate(string propertyName)
        {
            // Return error message if there is an error, else return empty or null string.
            switch (propertyName)
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

        public struct ConversionSetting
        {
            public ConversionSetting(KeyValuePair<string, string> keyValuePair)
            {
                this.Key = keyValuePair.Key;
                this.Value = keyValuePair.Value;
            }

            [XmlAttribute]
            public string Key
            {
                get;
                set;
            }

            [XmlAttribute]
            public string Value
            {
                get;
                set;
            }
        }
    }
}
