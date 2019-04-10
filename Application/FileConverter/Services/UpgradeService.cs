// <copyright file="UpgradeService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    using FileConverter.Annotations;
    using FileConverter.Diagnostics;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Ioc;

    public class UpgradeService : ObservableObject, IUpgradeService
    {
#if DEBUG
        private const string BaseURI = "https://raw.githubusercontent.com/Tichau/FileConverter/integration/";
#else
        private const string BaseURI = "https://raw.githubusercontent.com/Tichau/FileConverter/master/";
#endif

        [NotNull]
        private readonly WebClient webClient = new WebClient();

        private UpgradeVersionDescription upgradeVersionDescription;

        public UpgradeService()
        {
            this.UpgradeVersionDescription = new UpgradeVersionDescription();
            SimpleIoc.Default.Register<IUpgradeService>(() => this);
        }

        public event EventHandler<UpgradeVersionDescription> NewVersionAvailable;

        public UpgradeVersionDescription UpgradeVersionDescription
        {
            get => this.upgradeVersionDescription;
            private set
            {
                this.upgradeVersionDescription = value;
                this.RaisePropertyChanged();
            }
        }
        
        public async Task<UpgradeVersionDescription> CheckForUpgrade()
        {
            Task<UpgradeVersionDescription> task = null;
            try
            {
#if DEBUG
                task = this.DownloadLatestVersionDescription();
#else
                long fileTime = Registry.GetValue<long>(Registry.Keys.LastUpdateCheckDate);
                DateTime lastUpdateDateTime = DateTime.FromFileTime(fileTime);

                TimeSpan durationSinceLastUpdate = DateTime.Now.Subtract(lastUpdateDateTime);
                if (durationSinceLastUpdate > new TimeSpan(1, 0, 0, 0))
                {
                    task = this.DownloadLatestVersionDescription();
                }
#endif
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.Log($"Failed to check upgrade: {exception.Message}.");
            }

            UpgradeVersionDescription versionDescription = await task;

            if (versionDescription == null)
            {
                return null;
            }

            Registry.SetValue(Registry.Keys.LastUpdateCheckDate, DateTime.Now.ToFileTime());

            if (versionDescription.LatestVersion <= Application.ApplicationVersion)
            {
                return null;
            }

            this.UpgradeVersionDescription = versionDescription;

            this.NewVersionAvailable?.Invoke(this, versionDescription);
            return versionDescription;
        }

        public async Task<string> DownloadChangeLog()
        {
            if (this.UpgradeVersionDescription == null)
            {
                throw new ArgumentNullException(nameof(this.UpgradeVersionDescription));
            }

            this.UpgradeVersionDescription.ChangeLog = Properties.Resources.DownloadingChangeLog;

            Uri uri = new Uri(UpgradeService.BaseURI + "CHANGELOG.md");
            try
            {
                Task<Stream> openReadTaskAsync = this.webClient.OpenReadTaskAsync(uri);
                if (openReadTaskAsync == null)
                {
                    return null;
                }

                Stream stream = await openReadTaskAsync;
                using (StreamReader reader = new StreamReader(stream))
                {
                    this.UpgradeVersionDescription.ChangeLog = reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                Debug.LogError("Error while retrieving change log.");
                return null;
            }

            return this.UpgradeVersionDescription.ChangeLog;
        }

        public async Task StartUpgrade()
        {
            if (this.UpgradeVersionDescription == null)
            {
                Debug.Log("Can't start upgrade because no check upgrade have been done.");
                return;
            }

            try
            {
                this.UpgradeVersionDescription.NeedToUpgrade = true;
                await this.DownloadInstaller();
            }
            catch (Exception exception)
            {
                Debug.Log($"Failed to download upgrade: {exception.Message}.");
            }
        }

        public void CancelUpgrade()
        {
            if (this.UpgradeVersionDescription == null)
            {
                Debug.Log("Can't cancel upgrade because there is no upgrade in progress.");
                return;
            }

            Debug.Log("Cancel application upgrade.");
            this.UpgradeVersionDescription.NeedToUpgrade = false;
        }

        private async Task<UpgradeVersionDescription> DownloadLatestVersionDescription()
        {
#if BUILD32
            Uri uri = new Uri(Helpers.BaseURI + "version (x86).xml");
#else
            Uri uri = new Uri(UpgradeService.BaseURI + "version.xml");
#endif

            UpgradeVersionDescription description = null;
            try
            {
                Stream stream = await this.webClient.OpenReadTaskAsync(uri);

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
                    description = (UpgradeVersionDescription)serializer.Deserialize(xmlReader);
                }
            }
            catch (Exception)
            {
                Debug.Log("Error while retrieving change log.");
                return null;
            }

            return description;
        }

        private async Task DownloadInstaller()
        {
            if (this.UpgradeVersionDescription == null)
            {
                throw new ArgumentNullException(nameof(this.UpgradeVersionDescription));
            }

            if (this.UpgradeVersionDescription.InstallerDownloadInProgress)
            {
                throw new Exception("The installer download is currently in progress.");
            }

            Uri uri = new Uri(this.UpgradeVersionDescription.InstallerURL);

            string fileName = "FileConverter-setup.msi";
            Regex retrieveFileNameRegex = new Regex("/([^/]*)");
            MatchCollection matchCollection = retrieveFileNameRegex.Matches(this.UpgradeVersionDescription.InstallerURL);
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

            this.UpgradeVersionDescription.InstallerPath = installerPath;
            this.UpgradeVersionDescription.InstallerDownloadInProgress = true;
            this.UpgradeVersionDescription.InstallerDownloadProgress = 0;

            // Source: https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel#2904963
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            this.webClient.DownloadProgressChanged += this.WebClient_DownloadProgressChanged;

            try
            {
                await this.webClient.DownloadFileTaskAsync(uri, installerPath);

                this.UpgradeVersionDescription.InstallerDownloadProgress = 100;
                this.UpgradeVersionDescription.InstallerDownloadInProgress = false;
                this.UpgradeVersionDescription = null;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to download the new File Converter upgrade. You should try again or download it manually.");
                Debug.Log(exception.ToString());
                this.UpgradeVersionDescription.NeedToUpgrade = false;
            }

            this.webClient.DownloadProgressChanged -= this.WebClient_DownloadProgressChanged;
        }
        
        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs eventArgs)
        {
            this.UpgradeVersionDescription.InstallerDownloadProgress = eventArgs.ProgressPercentage;
        }
    }
}
