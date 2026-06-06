// <copyright file="DiagnosticsViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;

    using FileConverter.Services;

    /// <summary>
    /// This class contains properties that the diagnostics View can data bind to.
    /// </summary>
    public class DiagnosticsViewModel : ObservableRecipient
    {
        private RelayCommand copyDiagnosticsCommand;
        private RelayCommand<CancelEventArgs> closeCommand;
        private RelayCommand openDiagnosticsFolderCommand;

        /// <summary>
        /// Initializes a new instance of the DiagnosticsViewModel class.
        /// </summary>
        public DiagnosticsViewModel()
        {
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

        public ICommand CopyDiagnosticsCommand
        {
            get
            {
                if (this.copyDiagnosticsCommand == null)
                {
                    this.copyDiagnosticsCommand = new RelayCommand(this.CopyDiagnostics);
                }

                return this.copyDiagnosticsCommand;
            }
        }

        public ICommand OpenDiagnosticsFolderCommand
        {
            get
            {
                if (this.openDiagnosticsFolderCommand == null)
                {
                    this.openDiagnosticsFolderCommand = new RelayCommand(this.OpenDiagnosticsFolder);
                }

                return this.openDiagnosticsFolderCommand;
            }
        }

        private void Close(CancelEventArgs args)
        {
            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            navigationService.Close(Pages.Diagnostics, args != null);
        }

        private void CopyDiagnostics()
        {
            try
            {
                Clipboard.SetText(Diagnostics.Debug.AllContent);
            }
            catch (System.Exception exception)
            {
                Diagnostics.Debug.Log($"Can't copy diagnostics to clipboard: {exception.Message}.");
            }
        }

        private void OpenDiagnosticsFolder()
        {
            string diagnosticsFolderPath = Diagnostics.Debug.DiagnosticsFolderPath;
            if (string.IsNullOrEmpty(diagnosticsFolderPath) || !Directory.Exists(diagnosticsFolderPath))
            {
                Diagnostics.Debug.Log($"Can't open diagnostics folder: {diagnosticsFolderPath}.");
                return;
            }

            try
            {
                Process.Start("explorer.exe", $"\"{diagnosticsFolderPath}\"");
            }
            catch (System.Exception exception)
            {
                Diagnostics.Debug.Log($"Can't open diagnostics folder: {exception.Message}.");
            }
        }
    }
}
