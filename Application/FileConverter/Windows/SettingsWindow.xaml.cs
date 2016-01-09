// <copyright file="SettingsWindow.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using FileConverter.Annotations;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for Settings.
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private ConversionPreset selectedPreset;

        private Settings settings;

        private InputExtensionCategory[] inputCategories;

        public SettingsWindow()
        {
            this.InitializeComponent();
            
            Application application = Application.Current as Application;
            this.ApplicationSettings = application.Settings;
            this.PresetList.ItemsSource = this.settings.ConversionPresets;

            OutputType[] outputTypes = new[]
                                           {
                                               OutputType.Ogg, 
                                               OutputType.Mp3,
                                               OutputType.Aac,
                                               OutputType.Flac,
                                               OutputType.Wav,
                                               OutputType.Mkv,
                                               OutputType.Avi,
                                               OutputType.Ico,
                                               OutputType.Jpg,
                                               OutputType.Png,
                                           };

            this.OutputFormats.ItemsSource = outputTypes;

            InputPostConversionAction[] postConversionActions = new[]
                                           {
                                               InputPostConversionAction.None,
                                               InputPostConversionAction.MoveInArchiveFolder,
                                               InputPostConversionAction.Delete,
                                           };

            this.PostConversionActionComboBox.ItemsSource = postConversionActions;
            
            this.InitializeCompatibleInputExtensions();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event System.EventHandler<System.EventArgs> OnSettingsWindowHide;

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
                this.OnPropertyChanged("InputCategories");
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
                    if (this.SelectedPreset == null || PathHelpers.IsOutputTypeCompatibleWithCategory(this.SelectedPreset.OutputType, category.Name))
                    {
                        yield return category;
                    }
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectedPresetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "OutputType")
            {
                this.OnPropertyChanged("InputCategories");
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
            string presetName = "New preset";
            int index = 1;
            while (application.Settings.ConversionPresets.Any(match => match.Name == presetName))
            {
                index++;
                presetName = string.Format("New preset ({0})", index);
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
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
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
                string extensionCategory = PathHelpers.GetExtensionCategory(compatibleInputExtension);
                InputExtensionCategory category = categories.Find(match => match.Name == extensionCategory);
                if (category == null)
                {
                    category = new InputExtensionCategory(extensionCategory);
                    categories.Add(category);
                }

                category.AddExtension(compatibleInputExtension);
            }

            this.inputCategories = categories.ToArray();
            this.OnPropertyChanged("InputCategories");
        }
    }
}
