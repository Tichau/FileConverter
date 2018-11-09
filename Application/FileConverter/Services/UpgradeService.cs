
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
