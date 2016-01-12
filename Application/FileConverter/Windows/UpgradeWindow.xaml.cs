// <copyright file="UpgradeWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Windows
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;

    using FileConverter.Annotations;
    using FileConverter.Upgrade;
    
    public partial class UpgradeWindow : Window, INotifyPropertyChanged
    {
        private UpgradeVersionDescription upgradeVersionDescription;
        private string releaseNoteContent;

        public UpgradeWindow()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
                    return "###Downloading change log ...";
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

        private void OnInstallButtonClick(object sender, RoutedEventArgs e)
        {
            this.upgradeVersionDescription.NeedToUpgrade = true;
            Helpers.DownloadInstallerAsync(this.upgradeVersionDescription);
            this.Close();
        }
    }
}
