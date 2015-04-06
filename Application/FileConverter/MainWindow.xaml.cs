// <copyright file="MainWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using FileConverter.Annotations;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool verboseMode;

        private string applicationName;

        public MainWindow()
        {
            this.InitializeComponent();

            this.ApplicationName = string.Format("File Converter v{0}", Application.Version.ToString());

            Application application = Application.Current as Application;

            this.ConverterJobsList.ItemsSource = application.ConvertionJobs;
            this.VerboseMode = application.Verbose;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string ApplicationName
        {
            get
            {
                return this.applicationName;
            }

            private set
            {
                this.applicationName = value;
                this.OnPropertyChanged();
            }
        }

        public bool VerboseMode
        {
            get
            {
                return this.verboseMode;
            }

            set
            {
                this.verboseMode = value;
                this.OnPropertyChanged();
            }
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
    }
}
