// <copyright file="MainWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using FileConverter.Annotations;
    using FileConverter.Windows;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DiagnosticsWindow diagnosticsWindow;
        private SettingsWindow settingsWindow;
        private UpgradeWindow upgradeWindow;

        private string informationMessage;

        public MainWindow()
        {
            this.InitializeComponent();

            Application application = Application.Current as Application;

            application.OnApplicationTerminate += this.Application_OnApplicationTerminate;

            this.ConverterJobsList.ItemsSource = application.ConvertionJobs;

            if (application.HideMainWindow)
            {
                this.Hide();
            }

            if (application.Verbose)
            {
                this.ShowDiagnosticsWindow();
            }

            if (application.ShowSettings)
            {
                this.ShowSettingsWindow();
                this.settingsWindow.OnSettingsWindowHide += this.SettingsWindow_Closed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string InformationMessage
        {
            get
            {
                return this.informationMessage;
            }

            private set
            {
                this.informationMessage = value;
                this.OnPropertyChanged();
            }
        }

        public void OnNewVersionReleased(UpgradeVersionDescription upgradeVersionDescription)
        {
            this.ShowUpgradeWindow();
            this.upgradeWindow.VersionDescription = upgradeVersionDescription;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected override void OnClosing(CancelEventArgs eventArgs)
        {
            base.OnClosing(eventArgs);

            Application application = Application.Current as Application;

            if (application.UpgradeVersionDescription != null && application.UpgradeVersionDescription.NeedToUpgrade && !application.UpgradeVersionDescription.InstallerDownloadDone)
            {
                eventArgs.Cancel = true;
                this.ShowUpgradeWindow();
                this.upgradeWindow.VersionDescription = application.UpgradeVersionDescription;
            }
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.ShowSettingsWindow();
        }

        private void SettingsWindow_Closed(object sender, System.EventArgs e)
        {
            this.settingsWindow.OnSettingsWindowHide -= this.SettingsWindow_Closed;
            this.Close();
        }

        private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.ShowDiagnosticsWindow();
        }

        private void ShowDiagnosticsWindow()
        {
            if (this.diagnosticsWindow != null && this.diagnosticsWindow.IsVisible)
            {
                return;
            }
            else
            {
                this.diagnosticsWindow = new DiagnosticsWindow();
            }

            Application application = Application.Current as Application;
            application?.CancelAutoExit();
            this.diagnosticsWindow.Show();
        }

        private void ShowSettingsWindow()
        {
            if (this.settingsWindow != null && this.settingsWindow.IsVisible)
            {
                return;
            }
            else
            {
                this.settingsWindow = new SettingsWindow();
            }

            Application application = Application.Current as Application;
            application?.CancelAutoExit();
            this.settingsWindow.Show();
        }

        private void ShowUpgradeWindow()
        {
            if (this.upgradeWindow != null && this.upgradeWindow.IsVisible)
            {
                return;
            }
            else if (this.upgradeWindow == null)
            {
                this.upgradeWindow = new UpgradeWindow();
                this.upgradeWindow.Closed += UpgradeWindow_Closed;
            }
            
            Application application = Application.Current as Application;
            application?.CancelAutoExit();
            this.upgradeWindow.Show();
        }

        private void UpgradeWindow_Closed(object sender, System.EventArgs e)
        {
            Application application = Application.Current as Application;

            if (this.IsVisible)
            {
                return;
            }

            if (application.UpgradeVersionDescription != null && application.UpgradeVersionDescription.NeedToUpgrade && application.UpgradeVersionDescription.InstallerDownloadDone)
            {
                this.Close();
            }
        }

        private void Application_OnApplicationTerminate(object sender, ApplicationTerminateArgs eventArgs)
        {
            if (float.IsNaN(eventArgs.RemainingTimeBeforeTermination))
            {
                this.InformationMessage = string.Empty;
                return;
            }

            int remaingingSeconds = (int)eventArgs.RemainingTimeBeforeTermination;

            if (remaingingSeconds >= 2)
            {
                this.InformationMessage = string.Format(Properties.Resources.ApplicationWillTerminateInMultipleSeconds, remaingingSeconds);
            }
            else if (remaingingSeconds == 1)
            {
                this.InformationMessage = Properties.Resources.ApplicationWillTerminateInOneSecond;
            }

            if (remaingingSeconds <= 0)
            {
                this.InformationMessage = Properties.Resources.ApplicationIsTerminating;
            }
        }
    }
}
