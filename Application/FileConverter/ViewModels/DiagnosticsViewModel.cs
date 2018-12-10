// <copyright file="DiagnosticsViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.ComponentModel;
    using System.Windows.Input;

    using FileConverter.Services;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Ioc;

    /// <summary>
    /// This class contains properties that the diagnostics View can data bind to.
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
    public class DiagnosticsViewModel : ViewModelBase
    {
        private RelayCommand<CancelEventArgs> closeCommand;

        /// <summary>
        /// Initializes a new instance of the DiagnosticsViewModel class.
        /// </summary>
        public DiagnosticsViewModel()
        {
            if (this.IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
            }
            else
            {
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
            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Diagnostics, args != null);
        }
    }
}
