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

        public MainWindow()
        {
            this.InitializeComponent();

            Application application = Application.Current as Application;

            this.ConverterJobsList.ItemsSource = application.ConvertionJobs;
            if (application.Verbose)
            {
                this.ShowDiagnosticsWindow();
            }

            if (application.ShowSettings)
            {
                this.Hide();

                this.ShowSettingsWindow();
                this.settingsWindow.OnSettingsWindowHide += this.SettingsWindow_Closed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.ShowSettingsWindow();
        }

        private void SettingsWindow_Closed(object sender, System.EventArgs e)
        {
            settingsWindow.OnSettingsWindowHide -= this.SettingsWindow_Closed;
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
    }
}
