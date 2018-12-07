
namespace FileConverter.ViewModels
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using FileConverter.Commands;
    using FileConverter.ConversionJobs;
    using FileConverter.Services;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Ioc;

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class UpgradeViewModel : ViewModelBase
    {
        private UpgradeVersionDescription upgradeVersionDescription;
        private string releaseNoteContent;

        private RelayCommand downloadInstallerCommand;
        private RelayCommand launchInstallerCommand;

        /// <summary>
        /// Initializes a new instance of the UpgradeViewModel class.
        /// </summary>
        public UpgradeViewModel()
        {
            if (this.IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                UpgradeVersionDescription versionDescription = new UpgradeVersionDescription();
                versionDescription.LatestVersion = new Version() { Major = 0, Minor = 1, Patch = 0, };
                this.VersionDescription = versionDescription;
            }
            else
            {
                IUpgradeService upgradeService = SimpleIoc.Default.GetInstance<IUpgradeService>();
                this.VersionDescription = upgradeService.UpgradeVersionDescription;
                upgradeService.NewVersionAvailable += this.UpgradeService_NewVersionAvailable;
            }
        }

        public ICommand DownloadInstallerCommand
        {
            get
            {
                if (this.downloadInstallerCommand == null)
                {
                    this.downloadInstallerCommand = new RelayCommand(this.ExecuteDownloadInstallerCommand);
                }

                return this.downloadInstallerCommand;
            }
        }

        public ICommand LaunchInstallerCommand
        {
            get
            {
                if (this.launchInstallerCommand == null)
                {
                    this.launchInstallerCommand = new RelayCommand(this.ExecuteLaunchInstallerCommand);
                }

                return this.launchInstallerCommand;
            }
        }

        public UpgradeVersionDescription VersionDescription
        {
            get
            {
                return this.upgradeVersionDescription;
            }

            set
            {
                this.upgradeVersionDescription = value;

                Task<string> task = Upgrade.Helpers.GetChangeLogAsync(this.upgradeVersionDescription, this.OnChangeLogRetrieved);
                this.RaisePropertyChanged();
            }
        }

        public string ReleaseNote
        {
            get
            {
                if (string.IsNullOrEmpty(this.releaseNoteContent))
                {
                    return Properties.Resources.DownloadingChangeLog;
                }

                return this.releaseNoteContent;
            }

            set
            {
                this.releaseNoteContent = value;
                this.RaisePropertyChanged();
            }
        }

        private void UpgradeService_NewVersionAvailable(object sender, UpgradeVersionDescription newVersion)
        {
            this.VersionDescription = newVersion;
        }

        private void OnChangeLogRetrieved(UpgradeVersionDescription versionDescription)
        {
            this.ReleaseNote = versionDescription.ChangeLog;
        }

        private void ExecuteDownloadInstallerCommand()
        {
            IUpgradeService upgradeService = SimpleIoc.Default.GetInstance<IUpgradeService>();
            upgradeService.StartUpgrade();

            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.GoBack();
        }

        private void ExecuteLaunchInstallerCommand()
        {
            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.GoBack();
        }
    }
}
