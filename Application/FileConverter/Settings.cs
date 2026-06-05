// <copyright file="Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Linq;
    using System.Xml.Serialization;
    using System.Collections.ObjectModel;
    using System.Globalization;

    using CommunityToolkit.Mvvm.ComponentModel;

    [XmlRoot]
    [XmlType]
    public class Settings : ObservableObject, IXmlSerializable
    {
        public const int Version = 4;

        private bool exitApplicationWhenConversionsFinished = false;
        private float durationBetweenEndOfConversionsAndApplicationExit = 3f;
        private ObservableCollection<ConversionPreset> conversionPresets = new ObservableCollection<ConversionPreset>();
        private bool checkUpgradeAtStartup = true;
        private CultureInfo applicationLanguage;
        private int maximumNumberOfSimultaneousConversions;
        private bool copyFilesInClipboardAfterConversion = false;
        private Helpers.HardwareAccelerationMode hardwareAccelerationMode = Helpers.HardwareAccelerationMode.Off;

        public ConversionPreset GetPresetFromName(string presetName)
        {
            return this.conversionPresets.FirstOrDefault(match => match != null && match.FullName == presetName);
        }

        public void Clean()
        {
            for (int index = this.ConversionPresets.Count - 1; index >= 0; index--)
            {
                if (this.ConversionPresets[index] == null)
                {
                    this.ConversionPresets.RemoveAt(index);
                    continue;
                }

                this.ConversionPresets[index].Clean();
            }
        }
        
        public Settings Merge(Settings settings)
        {
            if (settings == null || settings.conversionPresets == null)
            {
                return this;
            }
            
            for (int index = 0; index < settings.conversionPresets.Count; index++)
            {
                ConversionPreset conversionPreset = settings.conversionPresets[index];
                if (conversionPreset == null)
                {
                    continue;
                }

                if (this.conversionPresets.Any(match => match != null && match.FullName == conversionPreset.FullName))
                {
                    continue;
                }

                this.conversionPresets.Add(conversionPreset);
            }

            return this;
        }

        [XmlAttribute]
        public int SerializationVersion
        {
            get;
            set;
        } = Version;

        [XmlIgnore]
        public CultureInfo ApplicationLanguage
        {
            get
            {
                return this.applicationLanguage;
            }

            set
            {
                if (this.applicationLanguage != null && this.applicationLanguage.Equals(value))
                {
                    return;
                }

                this.applicationLanguage = value;
                if (this.applicationLanguage != null)
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = this.applicationLanguage;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = this.applicationLanguage;
                }

                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public string ApplicationLanguageName
        {
            get
            {
                if (this.ApplicationLanguage == null)
                {
                    return string.Empty;
                }

                return this.ApplicationLanguage.Name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.ApplicationLanguage = null;
                    return;
                }

                try
                {
                    this.ApplicationLanguage = CultureInfo.GetCultureInfo(value);
                }
                catch (CultureNotFoundException)
                {
                    Diagnostics.Debug.Log($"Unsupported application language '{value}'. Fallback to default culture.");
                    this.ApplicationLanguage = null;
                }
            }
        }

        [XmlIgnore]
        public ObservableCollection<ConversionPreset> ConversionPresets
        {
            get
            {
                return this.conversionPresets;
            }

            set
            {
                this.conversionPresets = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public bool ExitApplicationWhenConversionsFinished
        {
            get
            {
                return this.exitApplicationWhenConversionsFinished;
            }

            set
            {
                this.exitApplicationWhenConversionsFinished = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public float DurationBetweenEndOfConversionsAndApplicationExit
        {
            get
            {
                return this.durationBetweenEndOfConversionsAndApplicationExit;
            }

            set
            {
                this.durationBetweenEndOfConversionsAndApplicationExit = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public int MaximumNumberOfSimultaneousConversions
        {
            get
            {
                return this.maximumNumberOfSimultaneousConversions;
            }

            set
            {
                this.maximumNumberOfSimultaneousConversions = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement("ConversionPreset")]
        public ConversionPreset[] SerializableConversionPresets
        {
            get
            {
                return this.ConversionPresets.ToArray();
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                for (int index = 0; index < value.Length; index++)
                {
                    if (value[index] == null)
                    {
                        continue;
                    }

                    this.ConversionPresets.Add(value[index]);
                }
            }
        }

        [XmlElement]
        public bool CheckUpgradeAtStartup
        {
            get
            {
                return this.checkUpgradeAtStartup;
            }

            set
            {
                this.checkUpgradeAtStartup = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public bool CopyFilesInClipboardAfterConversion
        {
            get
            {
                return this.copyFilesInClipboardAfterConversion;
            }

            set
            {
                this.copyFilesInClipboardAfterConversion = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public Helpers.HardwareAccelerationMode HardwareAccelerationMode
        {
            get
            {
                return this.hardwareAccelerationMode;
            }

            set
            {
                this.hardwareAccelerationMode = value;
                this.OnPropertyChanged();
            }
        }
        public void OnDeserializationComplete()
        {
            this.DurationBetweenEndOfConversionsAndApplicationExit = System.Math.Max(0, System.Math.Min(10, this.DurationBetweenEndOfConversionsAndApplicationExit));

            for (int index = this.ConversionPresets.Count - 1; index >= 0; index--)
            {
                if (this.ConversionPresets[index] == null)
                {
                    this.ConversionPresets.RemoveAt(index);
                    continue;
                }

                this.ConversionPresets[index].OnDeserializationComplete();
            }

            // Initialize application if it was not deserialized from the settings.
            if (this.ApplicationLanguage == null)
            {
                CultureInfo bestCandidate = null;
                CultureInfo currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
                foreach (CultureInfo culture in Helpers.GetSupportedCultures())
                {
                    if (culture.Equals(currentUICulture))
                    {
                        bestCandidate = culture;
                        break;
                    }
                    else if (culture.Equals(currentUICulture.Parent))
                    {
                        bestCandidate = culture;
                    }
                }

                if (bestCandidate != null)
                {
                    this.ApplicationLanguage = bestCandidate;
                }
                else
                {
                    Diagnostics.Debug.Log($"Can't find supported culture info for culture {currentUICulture}. Fallback to default culture.");
                    this.ApplicationLanguage = CultureInfo.GetCultureInfo("en");
                }
            }
        }
    }
}
