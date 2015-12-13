// <copyright file="Settings.Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.Linq;

namespace FileConverter
{
    using System.Collections.ObjectModel;
    using System.Xml.Serialization;

    public partial class Settings : IXmlSerializable
    {
        private bool quitApplicationWhenConversionsFinished = true;
        private float durationBetweenEndOfConversionsAndApplicationQuit = 3f;
        private ObservableCollection<ConversionPreset> conversionPresets = new ObservableCollection<ConversionPreset>();
        
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
        public bool QuitApplicationWhenConversionsFinished
        {
            get
            {
                return this.quitApplicationWhenConversionsFinished;
            }

            set
            {
                this.quitApplicationWhenConversionsFinished = value;
                this.OnPropertyChanged();
            }
        }

        [XmlElement]
        public float DurationBetweenEndOfConversionsAndApplicationQuit
        {
            get
            {
                return this.durationBetweenEndOfConversionsAndApplicationQuit;
            }

            set
            {
                this.durationBetweenEndOfConversionsAndApplicationQuit = value;
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
            for (int index = 0; index < this.ConversionPresets.Count; index++)
            {
                this.ConversionPresets[index].OnDeserializationComplete();
            }
        }
    }
}
