// <copyright file="UpgradeService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    using CommunityToolkit.Mvvm.ComponentModel;

    using FileConverter.Annotations;
    using FileConverter.Diagnostics;

    public class UpgradeService : ObservableObject, IUpgradeService
    {
#if DEBUG
        private const string BaseURI = "https://raw.githubusercontent.com/ZaidNAlAsali/FileConverter/integration/";
#else
        private const string BaseURI = "https://raw.githubusercontent.com/ZaidNAlAsali/FileConverter/master/";
#endif

        private const string ReleaseHost = "github.com";
        private const string ReleasePathPrefix = "/ZaidNAlAsali/FileConverter/releases/download/";

        [NotNull]
        private readonly WebClient webClient = new WebClient();

        private UpgradeVersionDescription upgradeVersionDescription;

        public UpgradeService()
        {
            this.UpgradeVersionDescription = new UpgradeVersionDescription();
        }

        public event EventHandler<UpgradeVersionDescription> NewVersionAvailable;

        public UpgradeVersionDescription UpgradeVersionDescription
        {
            get => this.upgradeVersionDescription;
            private set
            {
                this.upgradeVersionDescription = value;
                this.OnPropertyChanged();
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

            if (task == null)
            {
                return null;
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
            Uri uri = new Uri(UpgradeService.BaseURI + "version (x86).xml");
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
                Debug.Log("Error while retrieving version description.");
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

            if (!this.TryCreateTrustedInstallerUri(this.UpgradeVersionDescription.InstallerURL, out Uri uri, out string uriErrorMessage))
            {
                Debug.LogError($"Refuse to download upgrade installer. {uriErrorMessage}");
                this.UpgradeVersionDescription.NeedToUpgrade = false;
                return;
            }

            if (!this.IsValidSha256(this.UpgradeVersionDescription.InstallerSha256))
            {
                Debug.LogError("Refuse to download upgrade installer. The update manifest does not contain a valid SHA-256 hash.");
                this.UpgradeVersionDescription.NeedToUpgrade = false;
                return;
            }

            string fileName = Uri.UnescapeDataString(Path.GetFileName(uri.LocalPath));
            string tempPath = Path.Combine(Path.GetTempPath(), "ZFileConverter", Guid.NewGuid().ToString("N"));
            string installerPath = Path.Combine(tempPath, fileName);
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to prepare the temporary upgrade folder.");
                Debug.Log(exception.ToString());
                this.UpgradeVersionDescription.NeedToUpgrade = false;
                return;
            }

            this.UpgradeVersionDescription.InstallerPath = installerPath;
            this.UpgradeVersionDescription.InstallerIsVerified = false;
            this.UpgradeVersionDescription.InstallerDownloadInProgress = true;
            this.UpgradeVersionDescription.InstallerDownloadProgress = 0;

            // Source: https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel#2904963
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            this.webClient.DownloadProgressChanged += this.WebClient_DownloadProgressChanged;

            try
            {
                await this.webClient.DownloadFileTaskAsync(uri, installerPath);

                this.VerifyDownloadedInstaller(installerPath, this.UpgradeVersionDescription);

                this.UpgradeVersionDescription.InstallerDownloadProgress = 100;
                this.UpgradeVersionDescription.InstallerIsVerified = true;
                this.UpgradeVersionDescription.InstallerDownloadInProgress = false;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to download the new ZFileConverter upgrade. You should try again or download it manually.");
                Debug.Log(exception.ToString());
                this.UpgradeVersionDescription.InstallerDownloadInProgress = false;
                this.UpgradeVersionDescription.InstallerDownloadProgress = 0;
                this.UpgradeVersionDescription.InstallerIsVerified = false;
                this.UpgradeVersionDescription.NeedToUpgrade = false;
                this.DeleteInstallerIfExists(installerPath);
            }

            this.webClient.DownloadProgressChanged -= this.WebClient_DownloadProgressChanged;
        }

        private bool TryCreateTrustedInstallerUri(string installerUrl, out Uri uri, out string errorMessage)
        {
            uri = null;
            errorMessage = null;

            if (!Uri.TryCreate(installerUrl, UriKind.Absolute, out uri))
            {
                errorMessage = "The installer URL is not an absolute URL.";
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The installer URL must use HTTPS.";
                return false;
            }

            if (!string.Equals(uri.Host, ReleaseHost, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"The installer URL host must be {ReleaseHost}.";
                return false;
            }

            if (uri.AbsolutePath.IndexOf(ReleasePathPrefix, StringComparison.OrdinalIgnoreCase) < 0)
            {
                errorMessage = "The installer URL must point to a ZFileConverter GitHub release asset.";
                return false;
            }

            string fileName = Uri.UnescapeDataString(Path.GetFileName(uri.LocalPath));
            if (string.IsNullOrEmpty(fileName) ||
                !string.Equals(Path.GetExtension(fileName), ".msi", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The installer URL must point to an MSI package.";
                return false;
            }

            return true;
        }

        private bool IsValidSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool isHex =
                    (character >= '0' && character <= '9') ||
                    (character >= 'a' && character <= 'f') ||
                    (character >= 'A' && character <= 'F');

                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        private void VerifyDownloadedInstaller(string installerPath, UpgradeVersionDescription description)
        {
            string expectedSha256 = description.InstallerSha256.Replace(" ", string.Empty).ToUpperInvariant();
            string actualSha256 = this.ComputeSha256(installerPath);
            if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The upgrade installer SHA-256 hash did not match. Expected {expectedSha256}, actual {actualSha256}.");
            }

            if (string.IsNullOrWhiteSpace(description.InstallerPublisherSubject))
            {
                return;
            }

            X509Certificate2 certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(installerPath));
            if (certificate == null ||
                certificate.Subject.IndexOf(description.InstallerPublisherSubject, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("The upgrade installer publisher did not match the update manifest.");
            }
        }

        private string ComputeSha256(string filePath)
        {
            using (FileStream fileStream = File.OpenRead(filePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(fileStream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private void DeleteInstallerIfExists(string installerPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(installerPath) && File.Exists(installerPath))
                {
                    File.Delete(installerPath);
                }
            }
            catch (Exception exception)
            {
                Debug.Log($"Failed to delete invalid installer {installerPath}: {exception.Message}");
            }
        }
        
        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs eventArgs)
        {
            if (this.UpgradeVersionDescription == null)
            {
                return;
            }

            this.UpgradeVersionDescription.InstallerDownloadProgress = eventArgs.ProgressPercentage;
        }
    }
}
