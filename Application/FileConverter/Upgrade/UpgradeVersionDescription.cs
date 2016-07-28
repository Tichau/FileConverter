// <copyright file="UpgradeVersionDescription.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using System.ComponentModel;
using System.Runtime.CompilerServices;
using FileConverter.Annotations;

namespace FileConverter
{
    using System.Xml.Serialization;

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

        [XmlIgnore]
        public string InstallerURL
        {
            get
            {
#if BUILD32
                return this.InstallerX86URL;
#else
                return this.InstallerX64URL;
#endif
            }
        }

        [XmlElement("URL")]
        public string InstallerX64URL
        {
            get;
            set;
        }

        [XmlElement("URLx86")]
        public string InstallerX86URL
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