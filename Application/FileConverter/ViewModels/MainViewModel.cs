// <copyright file="MainViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Input;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;

    using FileConverter.ConversionJobs;
    using FileConverter.Services;

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public class MainViewModel : ObservableRecipient
    {
        private string informationMessage;
        private ObservableCollection<ConversionJob> conversionJobs;

        private RelayCommand showSettingsCommand;
        private RelayCommand showDiagnosticsCommand;
        private RelayCommand<CancelEventArgs> closeCommand;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            IConversionService settingsService = Ioc.Default.GetRequiredService<IConversionService>();
            this.ConversionJobs = new ObservableCollection<ConversionJob>(settingsService.ConversionJobs);

            Application application = Application.Current as Application;
            application.OnApplicationTerminate += this.Application_OnApplicationTerminate;
        }

        public string InformationMessage
        {
            get => this.informationMessage;

            private set
            {
                this.SetProperty(ref this.informationMessage, value);
            }
        }

        public ObservableCollection<ConversionJob> ConversionJobs
        {
            get => this.conversionJobs;

            private set
            {
                this.SetProperty(ref this.conversionJobs, value);

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
                    this.showSettingsCommand = new RelayCommand(() => Ioc.Default.GetRequiredService<INavigationService>().Show(Pages.Settings));
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
                    this.showDiagnosticsCommand = new RelayCommand(() => Ioc.Default.GetRequiredService<INavigationService>().Show(Pages.Diagnostics));
                }

                return this.showDiagnosticsCommand;
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

        private void Close(CancelEventArgs args)
        {
            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            navigationService.Close(Pages.Main, args != null);
        }

        private void ConversionJob_PropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName != "State" && eventArgs.PropertyName != "Progress")
            {
                return;
            }

            this.OnPropertyChanged(nameof(this.ConversionJobs));
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