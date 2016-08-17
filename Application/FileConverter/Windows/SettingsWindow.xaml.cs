// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;

    using FileConverter.Windows;
    using FileConverter.Annotations;
    using FileConverter.Commands;
    using FileConverter.ConversionJobs;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for Settings.
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private ConversionPreset selectedPreset;

        private Settings settings;

        private InputExtensionCategory[] inputCategories;
        private string releaseNoteContent;

        private OpenUrlCommand openUrlCommand;
        private DelegateCommand getChangeLogContentCommand;
        private bool displaySeeChangeLogLink = true;
        
        public SettingsWindow()
        {
            this.SupportedCultures = Helpers.GetSupportedCultures().Where(cultureInfo => cultureInfo.IsNeutralCulture).ToArray();

            this.InitializeComponent();
            
            Application application = Application.Current as Application;
            this.ApplicationSettings = application.Settings;
            this.PresetList.ItemsSource = this.settings.ConversionPresets;
            
            ListCollectionView collectionView = new ListCollectionView(new List<OutputTypeViewModel>(SettingsWindow.OutputTypeViewModels));
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            this.OutputFormats.ItemsSource = collectionView;

            this.InitializeCompatibleInputExtensions();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<EventArgs> OnSettingsWindowHide;

        public ICommand OpenUrlCommand
        {
            get
            {
                if (this.openUrlCommand == null)
                {
                    this.openUrlCommand = new OpenUrlCommand();
                }

                return this.openUrlCommand;
            }
        }

        public ICommand GetChangeLogContentCommand
        {
            get
            {
                if (this.getChangeLogContentCommand == null)
                {
                    this.getChangeLogContentCommand = new DelegateCommand(this.DownloadChangeLogAction);
                }

                return this.getChangeLogContentCommand;
            }
        }

        public ConversionPreset SelectedPreset
        {
            get
            {
                return this.selectedPreset;
            }

            set
            {
                if (this.selectedPreset != null)
                {
                    this.selectedPreset.PropertyChanged -= this.SelectedPresetPropertyChanged;
                }

                this.selectedPreset = value;

                if (this.selectedPreset != null)
                {
                    this.selectedPreset.PropertyChanged += this.SelectedPresetPropertyChanged;
                }

                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.InputCategories));
            }
        }

        public Settings ApplicationSettings
        {
            get
            {
                return this.settings;
            }

            set
            {
                this.settings = value;
                this.OnPropertyChanged();
            }
        }

        public IEnumerable<InputExtensionCategory> InputCategories
        {
            get
            {
                if (this.inputCategories == null)
                {
                    yield break;
                }

                for (int index = 0; index < this.inputCategories.Length; index++)
                {
                    InputExtensionCategory category = this.inputCategories[index];
                    if (this.SelectedPreset == null || Helpers.IsOutputTypeCompatibleWithCategory(this.SelectedPreset.OutputType, category.Name))
                    {
                        yield return category;
                    }
                }
            }
        }

        public CultureInfo[] SupportedCultures
        {
            get;
            set;
        }

        public static OutputTypeViewModel[] OutputTypeViewModels => new[]
        {
            new OutputTypeViewModel(OutputType.Ogg),
            new OutputTypeViewModel(OutputType.Mp3),
            new OutputTypeViewModel(OutputType.Aac),
            new OutputTypeViewModel(OutputType.Flac),
            new OutputTypeViewModel(OutputType.Wav),
            new OutputTypeViewModel(OutputType.Mkv),
            new OutputTypeViewModel(OutputType.Mp4),
            new OutputTypeViewModel(OutputType.Ogv),
            new OutputTypeViewModel(OutputType.Webm),
            new OutputTypeViewModel(OutputType.Avi),
            new OutputTypeViewModel(OutputType.Png),
            new OutputTypeViewModel(OutputType.Jpg),
            new OutputTypeViewModel(OutputType.Ico),
            new OutputTypeViewModel(OutputType.Gif),
            new OutputTypeViewModel(OutputType.Pdf),
        };

        public InputPostConversionAction[] InputPostConversionActions => new[]
        {
            InputPostConversionAction.None,
            InputPostConversionAction.MoveInArchiveFolder,
            InputPostConversionAction.Delete,
        };

        public VideoEncodingSpeed[] VideoEncodingSpeeds => new[]
        {
            VideoEncodingSpeed.UltraFast,
            VideoEncodingSpeed.SuperFast,
            VideoEncodingSpeed.VeryFast,
            VideoEncodingSpeed.Faster,
            VideoEncodingSpeed.Fast,
            VideoEncodingSpeed.Medium,
            VideoEncodingSpeed.Slow,
            VideoEncodingSpeed.Slower,
            VideoEncodingSpeed.VerySlow,
        };

        public string AboutSectionContent
        {
            get
            { 
                string content = Properties.Resources.LicenceHeader1 + "\n";
                content += Properties.Resources.LicenceHeader2 + "\n\n";
                content += Properties.Resources.LicenceHeader3 + "\n\n";
                content += this.releaseNoteContent;
                return content;
            }

            set
            {
                this.releaseNoteContent = value;

                this.OnPropertyChanged();
            }
        }

        public bool DisplaySeeChangeLogLink
        {
            get
            {
                return this.displaySeeChangeLogLink;
            }

            private set
            {
                this.displaySeeChangeLogLink = value;

                this.OnPropertyChanged();
            }
        }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DownloadChangeLogAction()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Upgrade.Helpers.GetChangeLogAsync(new UpgradeVersionDescription(), this.OnChangeLogRetrieved);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.AboutSectionContent = Properties.Resources.DownloadingChangeLog;
            this.DisplaySeeChangeLogLink = false;
        }

        private void OnChangeLogRetrieved(UpgradeVersionDescription description)
        {
            this.AboutSectionContent = description.ChangeLog;
        }

        private void SelectedPresetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "OutputType")
            {
                this.OnPropertyChanged(nameof(this.InputCategories));
            }
        }

        private void OnInputTypeChecked(object sender, RoutedEventArgs e)
        {
            if (this.selectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            this.selectedPreset.AddInputType(inputFormat);
        }

        private void OnInputTypeUnchecked(object sender, RoutedEventArgs e)
        {
            if (this.selectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (!checkBox.IsVisible)
            {
                return;
            }

            string inputFormat = (checkBox.Content as string).ToLowerInvariant();

            this.selectedPreset.RemoveInputType(inputFormat);
        }

        private void OnInputTypeCategoryChecked(object sender, RoutedEventArgs e)
        {
            if (this.selectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = System.Array.Find(this.inputCategories, match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                this.selectedPreset.AddInputType(inputExtension);
            }
        }

        private void OnInputTypeCategoryUnchecked(object sender, RoutedEventArgs e)
        {
            if (this.selectedPreset == null)
            {
                return;
            }

            CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            string categoryName = checkBox.Content as string;
            InputExtensionCategory category = System.Array.Find(this.inputCategories, match => match.Name == categoryName);
            if (category == null)
            {
                return;
            }

            foreach (string inputExtension in category.InputExtensionNames)
            {
                this.selectedPreset.RemoveInputType(inputExtension);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.HideSettingsWindow();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save changes.
            Application application = Application.Current as Application;
            application.Settings.Save();

            this.Hide();

            this.OnSettingsWindowHide?.Invoke(this, new EventArgs());
        }

        private void AddPresetButton_Click(object sender, RoutedEventArgs e)
        {
            Application application = Application.Current as Application;
            string presetName = Properties.Resources.DefaultPresetName;
            int index = 1;
            while (application.Settings.ConversionPresets.Any(match => match.Name == presetName))
            {
                index++;
                presetName = $"{Properties.Resources.DefaultPresetName} ({index})";
            }

            ConversionPreset newPreset = null;
            if (this.SelectedPreset != null)
            {
                newPreset = new ConversionPreset(presetName, this.SelectedPreset);
            }
            else
            {
                newPreset = new ConversionPreset(presetName, OutputType.Mkv, new string[0]);
            }

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

        private void CanSaveSettings(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.settings != null && string.IsNullOrEmpty(this.settings.Error);
        }
        
        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            this.HideSettingsWindow();
        }

        private void HideSettingsWindow()
        {
            // Load previous preset in order to cancel changes.
            Application application = Application.Current as Application;
            application.Settings = Settings.Load();

            this.Hide();

            this.OnSettingsWindowHide?.Invoke(this, new EventArgs());
        }

        private void MovePresetUpButton_Click(object sender, RoutedEventArgs e)
        {
            ConversionPreset presetToMove = this.SelectedPreset;
            if (presetToMove == null)
            {
                return;
            }
            
            int indexOfSelectedPreset = this.settings.ConversionPresets.IndexOf(presetToMove);
            int newIndexOfSelectedPreset = System.Math.Max(0, indexOfSelectedPreset - 1);
            
            this.settings.ConversionPresets.Move(indexOfSelectedPreset, newIndexOfSelectedPreset);
        }

        private void MovePresetDownButton_Click(object sender, RoutedEventArgs e)
        {
            ConversionPreset presetToMove = this.SelectedPreset;
            if (presetToMove == null)
            {
                return;
            }

            int indexOfSelectedPreset = this.settings.ConversionPresets.IndexOf(presetToMove);
            int newIndexOfSelectedPreset = System.Math.Min(this.settings.ConversionPresets.Count - 1, indexOfSelectedPreset + 1);

            this.settings.ConversionPresets.Move(indexOfSelectedPreset, newIndexOfSelectedPreset);
        }

        private void InitializeCompatibleInputExtensions()
        {
            RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            if (registryKey == null)
            {
                MessageBox.Show("Can't retrieve the list of compatible input extensions. (code 0x09)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string registryValue = registryKey.GetValue("CompatibleInputExtensions") as string;
            if (registryValue == null)
            {
                MessageBox.Show("Can't retrieve the list of compatible input extensions. (code 0x0A)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] compatibleInputExtensions = registryValue.Split(';');

            List<InputExtensionCategory> categories = new List<InputExtensionCategory>();
            for (int index = 0; index < compatibleInputExtensions.Length; index++)
            {
                string compatibleInputExtension = compatibleInputExtensions[index];
                string extensionCategory = Helpers.GetExtensionCategory(compatibleInputExtension);
                InputExtensionCategory category = categories.Find(match => match.Name == extensionCategory);
                if (category == null)
                {
                    category = new InputExtensionCategory(extensionCategory);
                    categories.Add(category);
                }

                category.AddExtension(compatibleInputExtension);
            }

            this.inputCategories = categories.ToArray();
            this.OnPropertyChanged(nameof(this.InputCategories));
        }
    }
}
