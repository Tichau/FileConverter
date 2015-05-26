// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.ComponentModel;
    using System.Windows.Controls;

    using FileConverter.Annotations;

    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private ConversionPreset selectedPreset;

        public SettingsWindow()
        {
            this.InitializeComponent();
            
            Application application = Application.Current as Application;
            this.PresetList.ItemsSource = application.Settings.ConversionPresets;

            OutputType[] outputTypes = new[]
                                           {
                                               OutputType.Flac, 
                                               OutputType.Mp3, 
                                               OutputType.Ogg, 
                                               OutputType.Wav, 
                                           };

            this.OutputFormats.ItemsSource = outputTypes;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConversionPreset SelectedPreset
        {
            get
            {
                return this.selectedPreset;
            }

            set
            {
                this.selectedPreset = value;
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

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (this.selectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            string inputFormat = checkBox.Content as string;

            if (!this.selectedPreset.InputTypes.Contains(inputFormat))
            {
                this.selectedPreset.InputTypes.Add(inputFormat);
            }
        }

        private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (this.selectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            string inputFormat = checkBox.Content as string;

            this.selectedPreset.InputTypes.Remove(inputFormat);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Load previous preset in order to cancel changes.
            Application application = Application.Current as Application;
            application.Settings.Load();

            this.Hide();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save changes.
            Application application = Application.Current as Application;
            application.Settings.Save();

            this.Hide();
        }

        private void AddPresetButton_Click(object sender, RoutedEventArgs e)
        {
            Application application = Application.Current as Application;
            ConversionPreset newPreset = new ConversionPreset("New preset", OutputType.None, new string[0]);
            application.Settings.ConversionPresets.Add(newPreset);
            this.SelectedPreset = newPreset;
            this.PresetNameTextBox.Focus();
            this.PresetNameTextBox.SelectAll();
        }

        private void RemovePresetButton_Click(object sender, RoutedEventArgs e)
        {
            Application application = Application.Current as Application;
            application.Settings.ConversionPresets.Remove(this.selectedPreset);
        }
    }
}
