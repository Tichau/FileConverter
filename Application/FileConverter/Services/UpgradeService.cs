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

        public delegate void OnUpgradeOperationCompletedEventHandler(UpgradeVersionDescription upgradeVersionDescription);

        public event System.EventHandler<UpgradeVersionDescription> NewVersionAvailable;

        public UpgradeVersionDescription UpgradeVersionDescription
        {
            get => this.upgradeVersionDescription;
            private set
            {
                this.upgradeVersionDescription = value;
                this.RaisePropertyChanged();
            }
        }
        
        public void CheckForUpgrade()
        {
            try
            {
#if DEBUG
                Task<UpgradeVersionDescription> task = this.GetLatestVersionDescriptionAsync(this.OnGetLatestVersionDescription);
#else
                long fileTime = Registry.GetValue<long>(Registry.Keys.LastUpdateCheckDate);
                DateTime lastUpdateDateTime = DateTime.FromFileTime(fileTime);

                TimeSpan durationSinceLastUpdate = DateTime.Now.Subtract(lastUpdateDateTime);
                if (durationSinceLastUpdate > new TimeSpan(1, 0, 0, 0))
                {
                    Task<UpgradeVersionDescription> task = this.GetLatestVersionDescriptionAsync(this.OnGetLatestVersionDescription);
                }
#endif
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.Log($"Failed to check upgrade: {exception.Message}.");
            }
        }

        public void StartUpgrade()
        {
            if (this.UpgradeVersionDescription == null)
            {
                Diagnostics.Debug.Log("Can't start upgrade because no check upgrade have been done.");
                return;
            }

            try
            {
                this.UpgradeVersionDescription.NeedToUpgrade = true;
                Task task = this.DownloadInstallerAsync();
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.Log($"Failed to download upgrade: {exception.Message}.");
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

        public async Task<string> GetChangeLogAsync(OnUpgradeOperationCompletedEventHandler onGetCompleteDelegate = null)
        {
            if (this.UpgradeVersionDescription == null)
            {
                throw new ArgumentNullException(nameof(this.UpgradeVersionDescription));
            }

            this.UpgradeVersionDescription.ChangeLog = Properties.Resources.DownloadingChangeLog;

            Uri uri = new Uri(UpgradeService.BaseURI + "CHANGELOG.md");
            try
            {
                Stream stream = await this.webClient.OpenReadTaskAsync(uri);
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

            onGetCompleteDelegate?.Invoke(this.UpgradeVersionDescription);

            return this.UpgradeVersionDescription.ChangeLog;
        }

        private void OnGetLatestVersionDescription(UpgradeVersionDescription description)
        {
            if (description == null)
            {
                return;
            }

            Registry.SetValue(Registry.Keys.LastUpdateCheckDate, DateTime.Now.ToFileTime());

            if (description.LatestVersion <= Application.ApplicationVersion)
            {
                return;
            }

            this.UpgradeVersionDescription = description;
            
            this.NewVersionAvailable?.Invoke(this, description);
        }

        private async Task<UpgradeVersionDescription> GetLatestVersionDescriptionAsync(OnUpgradeOperationCompletedEventHandler onGetCompleteDelegate = null)
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

            onGetCompleteDelegate?.Invoke(description);

            return description;
        }

        private async Task DownloadInstallerAsync()
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
            this.webClient.DownloadFileCompleted += this.WebClient_DownloadFileCompleted;
            Task downloadTask = this.webClient.DownloadFileTaskAsync(uri, installerPath);
            if (downloadTask != null)
            {
                await downloadTask;
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.webClient.DownloadProgressChanged -= this.WebClient_DownloadProgressChanged;
            this.webClient.DownloadFileCompleted -= this.WebClient_DownloadFileCompleted;

            if (e.Error != null)
            {
                Debug.LogError("Failed to download the new File Converter upgrade. You should try again or download it manually.");
                Debug.Log(e.Error.ToString());
                if (this.UpgradeVersionDescription != null)
                {
                    this.UpgradeVersionDescription.NeedToUpgrade = false;
                }
            }

            if (this.UpgradeVersionDescription != null)
            {
                this.UpgradeVersionDescription.InstallerDownloadProgress = 100;
                this.UpgradeVersionDescription.InstallerDownloadInProgress = false;
                this.UpgradeVersionDescription = null;
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs eventArgs)
        {
            if (this.UpgradeVersionDescription != null)
            {
                this.UpgradeVersionDescription.InstallerDownloadProgress = eventArgs.ProgressPercentage;
            }
        }
    }
}
