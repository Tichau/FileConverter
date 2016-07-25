// <copyright file="UpgradeWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Windows
{
    using System;
    using System.Windows.Input;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;

    using FileConverter.Annotations;
    using FileConverter.Commands;
    using FileConverter.Upgrade;
    
    public partial class UpgradeWindow : Window, INotifyPropertyChanged
    {
        private UpgradeVersionDescription upgradeVersionDescription;
        private string releaseNoteContent;

        private DelegateCommand downloadInstallerCommand;
        private DelegateCommand launchInstallerCommand;

        public UpgradeWindow()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand DownloadInstallerCommand
        {
            get
            {
                if (this.downloadInstallerCommand == null)
                {
                    this.downloadInstallerCommand = new DelegateCommand(this.ExecuteDownloadInstallerCommand);
                }

                return this.downloadInstallerCommand;
            }
        }

        public ICommand LaunchInstallerCommand
        {
            get
            {
                if (this.launchInstallerCommand == null)
                {
                    this.launchInstallerCommand = new DelegateCommand(this.ExecuteLaunchInstallerCommand);
                }

                return this.launchInstallerCommand;
            }
        }
        
        public UpgradeVersionDescription VersionDescription
        {
            get
            {
                return this.upgradeVersionDescription;
            }

            set
            {
                this.upgradeVersionDescription = value;

                Task<string> task = Helpers.GetChangeLogAsync(this.upgradeVersionDescription, this.OnChangeLogRetrieved);

                this.OnPropertyChanged();
            }
        }
        
        public string ReleaseNote
        {
            get
            {
                if (string.IsNullOrEmpty(this.releaseNoteContent))
                {
                    return Properties.Resources.DownloadingChangeLog;
                }

                return this.releaseNoteContent;
            }

            set
            {
                this.releaseNoteContent = value;

                this.OnPropertyChanged();
            }
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnChangeLogRetrieved(UpgradeVersionDescription versionDescription)
        {
            this.ReleaseNote = versionDescription.ChangeLog;
        }

        private void ExecuteDownloadInstallerCommand()
        {
            this.upgradeVersionDescription.NeedToUpgrade = true;
            Helpers.DownloadInstallerAsync(this.upgradeVersionDescription);
            this.Hide();
        }

        private void ExecuteLaunchInstallerCommand()
        {
            this.Close();
        }
    }
}
