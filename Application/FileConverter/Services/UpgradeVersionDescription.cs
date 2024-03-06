// <copyright file="UpgradeVersionDescription.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System.Xml.Serialization;

    using CommunityToolkit.Mvvm.ComponentModel;

    public class UpgradeVersionDescription : ObservableObject
    {
        private int installerDownloadProgress;
        private bool installerDownloadInProgress;

        private string changeLog;

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
            get => this.changeLog;
            set
            {
                this.changeLog = value;
                this.OnPropertyChanged();
            }
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
            get => this.installerDownloadInProgress;

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
            get => this.installerDownloadProgress;

            set
            {
                this.installerDownloadProgress = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.InstallerDownloadDone));
                this.OnPropertyChanged(nameof(this.InstallerDownloadNotStarted));
            }
        }

        [XmlIgnore]
        public bool InstallerDownloadDone => !this.InstallerDownloadInProgress && this.InstallerDownloadProgress == 100;

        [XmlIgnore]
        public bool InstallerDownloadNotStarted => !this.InstallerDownloadInProgress && this.InstallerDownloadProgress == 0;

        [XmlIgnore]
        public bool NeedToUpgrade
        {
            get;
            set;
        }
    }
}