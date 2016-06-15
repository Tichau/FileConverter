// <copyright file="Settings.Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace FileConverter
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Xml.Serialization;

    public partial class Settings : IXmlSerializable
    {
        public const int Version = 1;
        
        private bool exitApplicationWhenConversionsFinished = true;
        private float durationBetweenEndOfConversionsAndApplicationExit = 3f;
        private ObservableCollection<ConversionPreset> conversionPresets = new ObservableCollection<ConversionPreset>();
        private bool checkUpgradeAtStartup = true;
        private CultureInfo applicationLanguage;

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
                this.OnPropertyChanged();
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
