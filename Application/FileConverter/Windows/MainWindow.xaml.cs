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
        private bool verboseMode;

        public MainWindow()
        {
            this.InitializeComponent();

            Application application = Application.Current as Application;

            this.ConverterJobsList.ItemsSource = application.ConvertionJobs;
            if (application.Verbose)
            {
                DiagnosticsWindow window = new DiagnosticsWindow();
                window.Show();
            }

            if (application.ShowSettings)
            {
                this.Hide();
                SettingsWindow settingsWindow = new SettingsWindow();
                settingsWindow.Show();
                settingsWindow.Closed += SettingsWindow_Closed;
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
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void SettingsWindow_Closed(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void DiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DiagnosticsWindow window = new DiagnosticsWindow();
            window.Show();
        }
    }
}
