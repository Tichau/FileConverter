// <copyright file="InputExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;

    using FileConverter.Annotations;

    public class InputExtension : INotifyPropertyChanged
    {
        private readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 65, 0));

        private string name;
        private Brush foregroundBrush;
        private string toolTip;

        public InputExtension(string name)
        {
            this.Name = name;

            if (!Helpers.IsExtensionCompatibleWithOffice(name) || Helpers.IsMicrosoftOfficeAvailable())
            {
                this.ForegroundBrush = this.DefaultBrush;
            }
            else
            {
                this.ForegroundBrush = this.ErrorBrush;
                this.ToolTip = Properties.Resources.ErrorMicrosoftOfficeIsNotAvailable;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public Brush ForegroundBrush
        {
            get
            {
                return this.foregroundBrush;
            }

            set
            {
                this.foregroundBrush = value;
                this.OnPropertyChanged();
            }
        }

        public string ToolTip
        {
            get
            {
                return this.toolTip;
            }

            set
            {
                this.toolTip = value;
                this.OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}