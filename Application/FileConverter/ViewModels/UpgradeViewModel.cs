// <copyright file="UpgradeViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.ComponentModel;
    using System.Windows.Input;

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
        private readonly IUpgradeService upgradeService;

        private RelayCommand downloadInstallerCommand;
        private RelayCommand launchInstallerCommand;
        private RelayCommand<CancelEventArgs> closeCommand;

        /// <summary>
        /// Initializes a new instance of the UpgradeViewModel class.
        /// </summary>
        public UpgradeViewModel()
        {
            this.upgradeService = SimpleIoc.Default.GetInstance<IUpgradeService>();
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

            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Upgrade, false);
        }

        private void ExecuteLaunchInstallerCommand()
        {
            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Upgrade, false);
        }

        private void Close(CancelEventArgs args)
        {
            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Upgrade, args != null);
        }
    }
}
