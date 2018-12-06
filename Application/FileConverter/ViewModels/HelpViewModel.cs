
namespace FileConverter.ViewModels
{
    using System;
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
    public class HelpViewModel : ViewModelBase
    {
        private RelayCommand quitCommand;

        /// <summary>
        /// Initializes a new instance of the HelpViewModel class.
        /// </summary>
        public HelpViewModel()
        {
            if (this.IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
            }
            else
            {
            }
        }

        public ICommand QuitCommand
        {
            get
            {
                if (this.quitCommand == null)
                {
                    this.quitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
                }

                return this.quitCommand;
            }
        }
    }
}
