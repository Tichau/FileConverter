// <copyright file="UpgradeVersionDescription.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Xml.Serialization;

    public class UpgradeVersionDescription
    {
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
            get;
            set;
        }

        [XmlIgnore]
        public bool NeedToUpgrade
        {
            get;
            set;
        }
    }
}