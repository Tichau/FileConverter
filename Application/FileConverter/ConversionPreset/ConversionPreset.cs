// <copyright file="ConversionPreset.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Xml.Serialization;

    using FileConverter.Annotations;
    using FileConverter.Controls;
    using FileConverter.ValueConverters;

    [XmlRoot]
    [XmlType]
    public class ConversionPreset : INotifyPropertyChanged, IDataErrorInfo, IXmlSerializable
    {
        private string name;
        private OutputType outputType;
        private List<string> inputTypes;
        private InputPostConversionAction inputPostConversionAction;
        private ConversionSettings settings = new ConversionSettings();
        private string outputFileNameTemplate;
        private FileNameConverter outputFileNameConverter = new FileNameConverter();

        public ConversionPreset()
        {
            this.Name = "New Preset";
        }

        public ConversionPreset(string name, OutputType outputType, string[] inputTypes)
        {
            this.Name = name;
            this.OutputType = outputType;
            List<string> inputTypeList = new List<string>();
            inputTypeList.AddRange(inputTypes);
            this.InputTypes = inputTypeList;

            this.outputFileNameTemplate = "(p)(f)";
        }

        public ConversionPreset(string name, ConversionPreset source)
        {
            this.Name = name;
            this.OutputType = source.outputType;
            List<string> inputTypeList = new List<string>();
            if (source.inputTypes != null)
            {
                inputTypeList.AddRange(source.inputTypes);
            }

            this.InputTypes = inputTypeList;

            this.outputFileNameTemplate = source.OutputFileNameTemplate;

            foreach (KeyValuePair<string, string> conversionSetting in source.settings)
            {
                this.SetSettingsValue(conversionSetting.Key, conversionSetting.Value);
            }
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
                return this.outputType;
            }

            set
            {
                this.outputType = value;
                this.InitializeDefaultSettings(this.outputType);
                this.OnPropertyChanged();
                this.CoerceInputTypes();
            }
        }

        [XmlElement]
        public List<string> InputTypes
        {
            get
            {
                return this.inputTypes;
            }

            set
            {
                this.inputTypes = value;
                for (int index = 0; index < this.inputTypes.Count; index++)
                {
                    this.inputTypes[index] = this.inputTypes[index].ToLowerInvariant();
                }
                
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public InputPostConversionAction InputPostConversionAction
        {
            get
            {
                return this.inputPostConversionAction;
            }

            set
            {
                this.inputPostConversionAction = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement("Settings")]
        public ConversionSetting[] XmlSerializableSettings
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
                if (value != null)
                {
                    for (int index = 0; index < value.Length; index++)
                    {
                        // Compatibility issues.
                        if (value[index].Key == "Bitrate")
                        {
                            this.SetSettingsValue(ConversionSettingKeys.AudioBitrate, value[index].Value);
                            continue;
                        }

                        if (value[index].Key == "Encoding")
                        {
                            this.SetSettingsValue(ConversionSettingKeys.AudioEncodingMode, value[index].Value);
                            continue;
                        }

                        // Load settings.
                        this.SetSettingsValue(value[index].Key, value[index].Value);
                    }
                }

                this.OnPropertyChanged("Settings");
            }
        }

        [XmlElement]
        public string OutputFileNameTemplate
        {
            get
            {
                return this.outputFileNameTemplate;
            }

            set
            {
                this.outputFileNameTemplate = value;
                this.OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public string ConversionArchiveFolderName
        {
            get
            {
                return "Conversion Archives";
            }
        }

        [XmlIgnore]
        public IConversionSettings Settings
        {
            get
            {
                return this.settings;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, string> conversionSetting in value)
                {
                    if (!this.settings.ContainsKey(conversionSetting.Key))
                    {
                        this.settings.Add(conversionSetting.Key, conversionSetting.Value);
                    }
                    else
                    {
                        this.settings[conversionSetting.Key] = conversionSetting.Value;
                    }
                }

                this.OnPropertyChanged();
            }
        }

        public string Error
        {
            get
            {
                string errorString = this.Validate("Name");
                if (!string.IsNullOrEmpty(errorString))
                {
                    return errorString;
                }

                errorString = this.Validate("OutputFileNameTemplate");
                if (!string.IsNullOrEmpty(errorString))
                {
                    return errorString;
                }

                return string.Empty;
            }
        }

        public string this[string columnName]
        {
            get
            {
                return this.Validate(columnName);
            }
        }

        public void OnDeserializationComplete()
        {
            for (int index = 0; index < this.InputTypes.Count; index++)
            {
                this.InputTypes[index] = this.InputTypes[index].ToLowerInvariant();
            }

            this.CoerceInputTypes();
        }

        public void AddInputType(string inputType)
        {
            if (this.inputTypes.Contains(inputType))
            {
                return;
            }

            this.inputTypes.Add(inputType);
            this.OnPropertyChanged("InputTypes");
        }

        public void RemoveInputType(string inputType)
        {
            if (this.inputTypes.Remove(inputType))
            {
                this.OnPropertyChanged("InputTypes");
            }
        }

        public string GenerateOutputFilePath(string inputFilePath)
        {
            return (string)this.outputFileNameConverter.Convert(new object[] { inputFilePath, this.OutputType, this.OutputFileNameTemplate }, typeof(string), null, null);
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

            if (!this.IsRelevantSetting(settingsKey))
            {
                return;
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

            return null;
        }

        public T GetSettingsValue<T>(string settingsKey)
        {
            string settingsValue = this.GetSettingsValue(settingsKey);

            Type type = typeof(T);
            if (type.IsEnum)
            {
                return (T)System.Enum.Parse(type, settingsValue);
            }

            return (T)System.Convert.ChangeType(settingsValue, type);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CoerceInputTypes()
        {
            if (this.inputTypes == null)
            {
                return;
            }

            for (int index = 0; index < this.inputTypes.Count; index++)
            {
                string inputType = this.inputTypes[index];
                string inputCategory = PathHelpers.GetExtensionCategory(inputType);
                if (!PathHelpers.IsOutputTypeCompatibleWithCategory(this.OutputType, inputCategory))
                {
                    this.RemoveInputType(inputType);
                    index--;
                }
            }
        }

        private bool IsRelevantSetting(string settingsKey)
        {
            switch (this.OutputType)
            {
                case OutputType.Wav:
                    switch (settingsKey)
                    {
                        case ConversionPreset.ConversionSettingKeys.AudioEncodingMode:
                            return true;
                    }

                    break;

                case OutputType.Mp3:
                    switch (settingsKey)
                    {
                        case ConversionPreset.ConversionSettingKeys.AudioEncodingMode:
                        case ConversionPreset.ConversionSettingKeys.AudioBitrate:
                            return true;
                    }

                    break;

                case OutputType.Ogg:
                    switch (settingsKey)
                    {
                        case ConversionPreset.ConversionSettingKeys.AudioBitrate:
                            return true;
                    }

                    break;

                case OutputType.Mkv:
                    switch (settingsKey)
                    {
                        case ConversionPreset.ConversionSettingKeys.AudioBitrate:
                        case ConversionPreset.ConversionSettingKeys.VideoQuality:
                        case ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed:
                            return true;
                    }

                    break;

                case OutputType.Jpg:
                    switch (settingsKey)
                    {
                        case ConversionPreset.ConversionSettingKeys.ImageQuality:
                            return true;
                    }

                    break;
            }

            return false;
        }

        private void InitializeDefaultSettings(OutputType outputType)
        {
            if (outputType == OutputType.Wav)
            {
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.AudioEncodingMode, EncodingMode.Wav16.ToString());
            }

            if (outputType == OutputType.Mp3)
            {
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.AudioEncodingMode, EncodingMode.Mp3VBR.ToString());
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.AudioBitrate, "190");
            }

            if (outputType == OutputType.Ogg)
            {
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.AudioBitrate, "160");
            }

            if (outputType == OutputType.Flac)
            {
            }

            if (outputType == OutputType.Mkv)
            {
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.VideoQuality, "28");
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.VideoEncodingSpeed, "Very Slow");
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.AudioBitrate, "128");
            }

            if (outputType == OutputType.Png)
            {
            }

            if (outputType == OutputType.Jpg)
            {
                this.InitializeSettingsValue(ConversionPreset.ConversionSettingKeys.ImageQuality, "25");
            }

            this.OnPropertyChanged("Settings");
        }

        private void InitializeSettingsValue(string settingsKey, string value)
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
                            return "The preset name can't contains the character ';'.";
                        }

                        Application application = Application.Current as Application;
                        int? count = application?.Settings?.ConversionPresets?.Count(match => match?.name == this.Name);
                        if (count > 1)
                        {
                            return "The preset name is already used.";
                        }
                    }

                    break;

                case "OutputFileNameTemplate":
                    {
                        string sampleOutputFilePath = this.GenerateOutputFilePath(FileConverter.Properties.Resources.OuputFileNameTemplateSample);
                        if (string.IsNullOrEmpty(sampleOutputFilePath))
                        {
                            return "The output filename template must produce a non empty result.";
                        }

                        if (!PathHelpers.IsPathValid(sampleOutputFilePath))
                        {
                            // Diagnostic to feedback purpose.
                            // Drive letter.
                            if (!PathHelpers.IsPathDriveLetterValid(sampleOutputFilePath))
                            {
                                return "The output filename template must define a root (for example c:\\, use (p) to use the input file path).";
                            }

                            // File name.
                            string filename = PathHelpers.GetFileName(sampleOutputFilePath);
                            if (filename == null)
                            {
                                return "The output file name must not be empty (use (f) to use the name of the input file).";
                            }

                            char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                            for (int index = 0; index < invalidFileNameChars.Length; index++)
                            {
                                if (filename.Contains(invalidFileNameChars[index]))
                                {
                                    return "The output file name must not contains the character '" + invalidFileNameChars[index] + "'.";
                                }
                            }

                            // Directory names.
                            string path = sampleOutputFilePath.Substring(3, sampleOutputFilePath.Length - 3 - filename.Length);
                            char[] invalidPathChars = System.IO.Path.GetInvalidPathChars();
                            for (int index = 0; index < invalidPathChars.Length; index++)
                            {
                                if (string.IsNullOrEmpty(path))
                                {
                                    return "The output directory name must not be empty (use (d0), (d1), ... to use the name of the parent directories of the input file).";
                                }

                                if (path.Contains(invalidPathChars[index]))
                                {
                                    return "The output directory name must not contains the character '" + invalidPathChars[index] + "'.";
                                }
                            }

                            string[] directories = path.Split('\\');
                            for (int index = 0; index < directories.Length; ++index)
                            {
                                string directoryName = directories[index];
                                if (string.IsNullOrEmpty(directoryName))
                                {
                                    return "The output directory name must not be empty (use (d0), (d1), ... to use the name of the parent directories of the input file).";
                                }
                            }

                            return "The output filename template is invalid";
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

        public struct ConversionSettingKeys
        {
            public const string AudioEncodingMode = "AudioEncodingMode";
            public const string AudioBitrate = "AudioBitrate";
            public const string ImageQuality = "ImageQuality";
            public const string VideoQuality = "VideoQuality";
            public const string VideoEncodingSpeed = "VideoEncodingSpeed";
        }
    }
}
