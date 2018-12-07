// <copyright file="UpgradeService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.Threading.Tasks;
    
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Ioc;

    public class UpgradeService : ObservableObject, IUpgradeService
    {
        private UpgradeVersionDescription upgradeVersionDescription;

        public UpgradeService()
        {
            SimpleIoc.Default.Register<IUpgradeService>(() => this);
        }

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
                Task<UpgradeVersionDescription> task = Upgrade.Helpers.GetLatestVersionDescriptionAsync(this.OnGetLatestVersionDescription);
#else
                long fileTime = Registry.GetValue<long>(Registry.Keys.LastUpdateCheckDate);
                DateTime lastUpdateDateTime = DateTime.FromFileTime(fileTime);

                TimeSpan durationSinceLastUpdate = DateTime.Now.Subtract(lastUpdateDateTime);
                if (durationSinceLastUpdate > new TimeSpan(1, 0, 0, 0))
                {
                    Task<UpgradeVersionDescription> task = Upgrade.Helpers.GetLatestVersionDescriptionAsync(this.OnGetLatestVersionDescription);
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
                Task task = Upgrade.Helpers.DownloadInstallerAsync(this.upgradeVersionDescription);
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
                Diagnostics.Debug.Log("Can't cancel upgrade because there is no upgrade in progress.");
                return;
            }

            Diagnostics.Debug.Log("Cancel application upgrade.");
            this.UpgradeVersionDescription.NeedToUpgrade = false;
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
    }
}
