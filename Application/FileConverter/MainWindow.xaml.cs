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

        public MainWindow()
        {
            this.InitializeComponent();

            Application application = Application.Current as Application;

            this.ConverterJobsList.ItemsSource = application.ConvertionJobs;
            this.VerboseMode = application.Verbose;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
