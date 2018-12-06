
namespace FileConverter.ViewModels
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
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
    public class MainViewModel : ViewModelBase
    {
        private string informationMessage;
        private ObservableCollection<ConversionJob> conversionJobs;

        private RelayCommand showSettingsCommand;
        private RelayCommand showDiagnosticsCommand;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (this.IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                this.InformationMessage = "Design time mode.";
                this.ConversionJobs = new ObservableCollection<ConversionJob>();
                //this.ConversionJobs.Add(new ConversionJob());
            }
            else
            {
                IConversionService settingsService = SimpleIoc.Default.GetInstance<IConversionService>();
                this.ConversionJobs = new ObservableCollection<ConversionJob>(settingsService.ConversionJobs);

                Application application = Application.Current as Application;
                application.OnApplicationTerminate += this.Application_OnApplicationTerminate;
            }
        }

        public string InformationMessage
        {
            get
            {
                return this.informationMessage;
            }

            private set
            {
                this.Set(ref this.informationMessage, value);
            }
        }

        public ObservableCollection<ConversionJob> ConversionJobs
        {
            get
            {
                return this.conversionJobs;
            }

            private set
            {
                this.Set(ref this.conversionJobs, value);

                foreach (var job in this.conversionJobs)
                {
                    job.PropertyChanged += this.ConversionJob_PropertyChanged;
                }
            }
        }

        public ICommand ShowSettingsCommand
        {
            get
            {
                if (this.showSettingsCommand == null)
                {
                    this.showSettingsCommand = new RelayCommand(() => SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo(Pages.Settings));
                }

                return this.showSettingsCommand;
            }
        }

        public ICommand ShowDiagnosticsCommand
        {
            get
            {
                if (this.showDiagnosticsCommand == null)
                {
                    this.showDiagnosticsCommand = new RelayCommand(() => SimpleIoc.Default.GetInstance<INavigationService>().NavigateTo(Pages.Diagnostics));
                }

                return this.showDiagnosticsCommand;
            }
        }

        private void ConversionJob_PropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName != "State" && eventArgs.PropertyName != "Progress")
            {
                return;
            }

            this.RaisePropertyChanged(nameof(this.ConversionJobs));
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