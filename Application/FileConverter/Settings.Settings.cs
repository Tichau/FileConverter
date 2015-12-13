// <copyright file="Settings.Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Xml.Serialization;

    public partial class Settings : IXmlSerializable
    {
        private double settingsWindowHeight = 640;
        private double settingsWindowWidth = 800;
        private bool exitApplicationWhenConversionsFinished = true;
        private float durationBetweenEndOfConversionsAndApplicationExit = 3f;
        private ObservableCollection<ConversionPreset> conversionPresets = new ObservableCollection<ConversionPreset>();

        [XmlAttribute]
        public int SerializationVersion
        {
            get;
            set;
        } = 0;

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

        public void OnDeserializationComplete()
        {
            this.DurationBetweenEndOfConversionsAndApplicationExit = System.Math.Max(0, System.Math.Min(10, this.DurationBetweenEndOfConversionsAndApplicationExit));

            for (int index = 0; index < this.ConversionPresets.Count; index++)
            {
                this.ConversionPresets[index].OnDeserializationComplete();
            }
        }
    }
}
