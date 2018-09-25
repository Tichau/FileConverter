// <copyright file="Helpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Upgrade
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    public static class Helpers
    {
#if DEBUG
        private const string BaseURI = "https://raw.githubusercontent.com/Tichau/FileConverter/development/";
#else
        private const string BaseURI = "https://raw.githubusercontent.com/Tichau/FileConverter/master/";
#endif

        private static WebClient webClient = new WebClient();
        private static UpgradeVersionDescription currentlyDownloadedVersionDescription;

        public delegate void OnUpgradeOperationCompletedEventHandler(UpgradeVersionDescription upgradeVersionDescription);

        public static async Task<UpgradeVersionDescription> GetLatestVersionDescriptionAsync(OnUpgradeOperationCompletedEventHandler onGetCompleteDelegate = null)
        {
#if BUILD32
            Uri uri = new Uri(Helpers.BaseURI + "version (x86).xml");
#else
            Uri uri = new Uri(Helpers.BaseURI + "version.xml");
#endif

            Stream stream = await Helpers.webClient.OpenReadTaskAsync(uri);

            UpgradeVersionDescription upgradeVersionDescription = null;
            try
            {
                XmlRootAttribute xmlRoot = new XmlRootAttribute
                {
                    ElementName = "Version"
                };

                XmlSerializer serializer = new XmlSerializer(typeof(UpgradeVersionDescription), xmlRoot);

                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                };

                using (XmlReader xmlReader = XmlReader.Create(stream, xmlReaderSettings))
                {
                    upgradeVersionDescription = (UpgradeVersionDescription)serializer.Deserialize(xmlReader);
                }
            }
            catch (Exception)
            {
                Diagnostics.Debug.LogError("Error while retrieving change log.");
                return null;
            }

            onGetCompleteDelegate?.Invoke(upgradeVersionDescription);

            return upgradeVersionDescription;
        }

        public static async Task<string> GetChangeLogAsync(UpgradeVersionDescription upgradeVersionDescription, OnUpgradeOperationCompletedEventHandler onGetCompleteDelegate = null)
        {
            if (upgradeVersionDescription == null)
            {
                throw new ArgumentNullException(nameof(upgradeVersionDescription));
            }

            Uri uri = new Uri(Helpers.BaseURI + "CHANGELOG.md");
            Stream stream = await Helpers.webClient.OpenReadTaskAsync(uri);
            try
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    upgradeVersionDescription.ChangeLog = reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                Diagnostics.Debug.LogError("Error while retrieving change log.");
                return null;
            }

            onGetCompleteDelegate?.Invoke(upgradeVersionDescription);

            return upgradeVersionDescription.ChangeLog;
        }

        public static async Task DownloadInstallerAsync(UpgradeVersionDescription upgradeVersionDescription)
        {
            if (upgradeVersionDescription == null)
            {
                throw new ArgumentNullException(nameof(upgradeVersionDescription));
            }

            if (Helpers.currentlyDownloadedVersionDescription != null)
            {
                throw new Exception("The installer download is currently in progress.");
            }

            Uri uri = new Uri(upgradeVersionDescription.InstallerURL);

            string fileName = "FileConverter-setup.msi";
            Regex retrieveFileNameRegex = new Regex("/([^/]*)");
            MatchCollection matchCollection = retrieveFileNameRegex.Matches(upgradeVersionDescription.InstallerURL);
            if (matchCollection.Count > 0)
            {
                Match match = matchCollection[matchCollection.Count - 1];
                if (match.Groups.Count > 1)
                {
                    fileName = match.Groups[1].Value;
                }
            }

            string tempPath = System.IO.Path.GetTempPath();
            string installerPath = System.IO.Path.Combine(tempPath, fileName);

            upgradeVersionDescription.InstallerPath = installerPath;
            upgradeVersionDescription.InstallerDownloadInProgress = true;
            upgradeVersionDescription.InstallerDownloadProgress = 0;

            Helpers.currentlyDownloadedVersionDescription = upgradeVersionDescription;

            Helpers.webClient.DownloadProgressChanged += Helpers.WebClient_DownloadProgressChanged;
            Helpers.webClient.DownloadFileCompleted += Helpers.WebClient_DownloadFileCompleted;
            Task downloadTask = Helpers.webClient.DownloadFileTaskAsync(uri, installerPath);
        }

        private static void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Helpers.webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
            Helpers.webClient.DownloadFileCompleted -= WebClient_DownloadFileCompleted;

            if (Helpers.currentlyDownloadedVersionDescription != null)
            {
                Helpers.currentlyDownloadedVersionDescription.InstallerDownloadProgress = 100;
                Helpers.currentlyDownloadedVersionDescription.InstallerDownloadInProgress = false;
                Helpers.currentlyDownloadedVersionDescription = null;
            }
        }

        private static void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs eventArgs)
        {
            if (Helpers.currentlyDownloadedVersionDescription != null)
            {
                Helpers.currentlyDownloadedVersionDescription.InstallerDownloadProgress = eventArgs.ProgressPercentage;
            }
        }
    }
}
