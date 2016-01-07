// <copyright file="Helpers.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Upgrade
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    public static class Helpers
    {
#if DEBUG
        // https://github.com/Tichau/FileConverter/blob/development/version.xml
        private const string BaseURI = "file:///C:/Users/Adrien/Documents/GitHub/FileConverter/";
#else
        private const string BaseURI = "https://github.com/Tichau/FileConverter/blob/master/";
#endif

        private static WebClient webClient = new WebClient();

        public delegate void OnUpgradeOperationCompletedEventHandler(UpgradeVersionDescription upgradeVersionDescription);

        public static async Task<UpgradeVersionDescription> GetLatestVersionDescriptionAsync(OnUpgradeOperationCompletedEventHandler onGetCompleteDelegate = null)
        {
            Uri uri = new Uri(Helpers.BaseURI + "version.xml");
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
                throw new ArgumentNullException("upgradeVersionDescription");
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

        public static void DownloadInstallerAsync(UpgradeVersionDescription upgradeVersionDescription)
        {
            Thread thread = new Thread(Helpers.DownloadInstallerAsync);
            thread.Start(upgradeVersionDescription);
        }

        private static void DownloadInstallerAsync(object parameter)
        {
            UpgradeVersionDescription upgradeVersionDescription = parameter as UpgradeVersionDescription;

            if (upgradeVersionDescription == null)
            {
                throw new ArgumentNullException("upgradeVersionDescription");
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

            Helpers.webClient.DownloadFile(uri, installerPath);

            upgradeVersionDescription.InstallerDownloadInProgress = false;
        }
    }
}
