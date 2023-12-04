// <copyright file="Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;
    using System.Collections.ObjectModel;
    using System.Globalization;

    using GalaSoft.MvvmLight;

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
        private bool checkCopyFileAfterConverting = false;

        public ConversionPreset GetPresetFromName(string presetName)
        {
            return this.conversionPresets.FirstOrDefault(match => match.FullName == presetName);
        }

        public void Clean()
        {
            for (int index = 0; index < this.ConversionPresets.Count; index++)
            {
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
                if (this.conversionPresets.Any(match => match.FullName == conversionPreset.FullName))
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

                this.RaisePropertyChanged();
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

                this.ApplicationLanguage = CultureInfo.GetCultureInfo(value);
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
                this.RaisePropertyChanged();
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
                this.RaisePropertyChanged();
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
                this.RaisePropertyChanged();
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
                this.RaisePropertyChanged();
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
                for (int index = 0; index < value.Length; index++)
                {
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
                this.RaisePropertyChanged();
            }
        }

        [XmlElement]
        public bool CheckCopyFileAfterConverting
        {
            get
            {
                return this.checkCopyFileAfterConverting;
            }

            set
            {
                this.checkCopyFileAfterConverting = value;
                this.RaisePropertyChanged();
            }
        }
        public void OnDeserializationComplete()
        {
            this.DurationBetweenEndOfConversionsAndApplicationExit = System.Math.Max(0, System.Math.Min(10, this.DurationBetweenEndOfConversionsAndApplicationExit));

            for (int index = 0; index < this.ConversionPresets.Count; index++)
            {
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
                    Diagnostics.Debug.Log("Can't find supported culture info for culture {0}. Fallback to default culture.", currentUICulture);
                    this.ApplicationLanguage = CultureInfo.GetCultureInfo("en");
                }
            }
        }
    }
}
