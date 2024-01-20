// <copyright file="UpgradeViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.ComponentModel;
    using System.Windows.Input;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;

    using FileConverter.Services;

    /// <summary>
    /// This class contains properties that the upgrade View can data bind to.
    /// </summary>
    public class UpgradeViewModel : ObservableRecipient
    {
        private readonly IUpgradeService upgradeService;

        private RelayCommand downloadInstallerCommand;
        private RelayCommand launchInstallerCommand;
        private RelayCommand<CancelEventArgs> closeCommand;

        /// <summary>
        /// Initializes a new instance of the UpgradeViewModel class.
        /// </summary>
        public UpgradeViewModel()
        {
            this.upgradeService = Ioc.Default.GetRequiredService<IUpgradeService>();
            this.upgradeService.DownloadChangeLog();
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
        
        public ICommand CloseCommand
        {
            get
            {
                if (this.closeCommand == null)
                {
                    this.closeCommand = new RelayCommand<CancelEventArgs>(this.Close);
                }

                return this.closeCommand;
            }
        }

        private void ExecuteDownloadInstallerCommand()
        {
            this.upgradeService.StartUpgrade();

            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            navigationService.Close(Pages.Upgrade, false);
        }

        private void ExecuteLaunchInstallerCommand()
        {
            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            navigationService.Close(Pages.Upgrade, false);
        }

        private void Close(CancelEventArgs args)
        {
            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            navigationService.Close(Pages.Upgrade, args != null);
        }
    }
}
