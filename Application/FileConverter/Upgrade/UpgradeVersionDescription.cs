// <copyright file="UpgradeVersionDescription.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Xml.Serialization;

    using FileConverter.Annotations;

    public class UpgradeVersionDescription : INotifyPropertyChanged
    {
        private int installerDownloadProgress;
        private bool installerDownloadInProgress;

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlElement("Latest")]
        public Version LatestVersion
        {
            get;
            set;
        }
        
        [XmlElement("URL")]
        public string InstallerURL
        {
            get;
            set;
        }
        
        [XmlIgnore]
        public string ChangeLog
        {
            get;
            set;
        }

        [XmlIgnore]
        public string InstallerPath
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool InstallerDownloadInProgress
        {
            get
            {
                return this.installerDownloadInProgress;
            }

            set
            {
                this.installerDownloadInProgress = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.InstallerDownloadDone));
                this.OnPropertyChanged(nameof(this.InstallerDownloadNotStarted));
            }
        }

        [XmlIgnore]
        public int InstallerDownloadProgress
        {
            get
            {
                return this.installerDownloadProgress;
            }

            set
            {
                this.installerDownloadProgress = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.InstallerDownloadDone));
                this.OnPropertyChanged(nameof(this.InstallerDownloadNotStarted));
            }
        }

        [XmlIgnore]
        public bool InstallerDownloadDone
        {
            get
            {
                return !this.InstallerDownloadInProgress && this.InstallerDownloadProgress == 100;
            }
        }

        [XmlIgnore]
        public bool InstallerDownloadNotStarted
        {
            get
            {
                return !this.InstallerDownloadInProgress && this.InstallerDownloadProgress == 0;
            }
        }

        [XmlIgnore]
        public bool NeedToUpgrade
        {
            get;
            set;
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}