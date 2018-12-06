// <copyright file="MainWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Views
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using FileConverter.Annotations;
    using FileConverter.Services;

    using GalaSoft.MvvmLight.Ioc;

    using Application = FileConverter.Application;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private UpgradeWindow upgradeWindow;
        
        public MainWindow()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosing(CancelEventArgs eventArgs)
        {
            base.OnClosing(eventArgs);

            IUpgradeService upgradeService = SimpleIoc.Default.GetInstance<IUpgradeService>();

            if (upgradeService.UpgradeVersionDescription != null &&
                upgradeService.UpgradeVersionDescription.NeedToUpgrade &&
                !upgradeService.UpgradeVersionDescription.InstallerDownloadDone)
            {
                eventArgs.Cancel = true;

                INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
                navigationService.NavigateTo(Pages.Upgrade);
            }
        }

        //private void ShowUpgradeWindow()
        //{
        //    if (this.upgradeWindow != null && this.upgradeWindow.IsVisible)
        //    {
        //        return;
        //    }
        //    else if (this.upgradeWindow == null)
        //    {
        //        this.upgradeWindow = new UpgradeWindow();
        //        this.upgradeWindow.Closed += this.UpgradeWindow_Closed;
        //    }

        //    Application application = Application.Current as Application;
        //    application?.CancelAutoExit();
        //    this.upgradeWindow.Show();
        //}

        //private void UpgradeWindow_Closed(object sender, System.EventArgs e)
        //{
        //    Application application = Application.Current as Application;

        //    if (this.IsVisible)
        //    {
        //        return;
        //    }

        //    IUpgradeService upgradeService = SimpleIoc.Default.GetInstance<IUpgradeService>();
        //    if (upgradeService.UpgradeVersionDescription != null && 
        //        upgradeService.UpgradeVersionDescription.NeedToUpgrade && 
        //        upgradeService.UpgradeVersionDescription.InstallerDownloadDone)
        //    {
        //        this.Close();
        //    }
        //}
    }
}
