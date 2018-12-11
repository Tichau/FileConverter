// <copyright file="SettingsViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;

    using FileConverter.Services;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Ioc;
    using GalaSoft.MvvmLight.Messaging;

    using Microsoft.Win32;

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
    public class SettingsViewModel : ViewModelBase
    {
        private InputExtensionCategory[] inputCategories;
        private ConversionPreset selectedPreset;
        private Settings settings;
        private string releaseNoteContent;
        private bool displaySeeChangeLogLink = true;

        private RelayCommand<string> openUrlCommand;
        private RelayCommand getChangeLogContentCommand;
        private RelayCommand movePresetUpCommand;
        private RelayCommand movePresetDownCommand;
        private RelayCommand addNewPresetCommand;
        private RelayCommand removePresetCommand;
        private RelayCommand saveCommand;
        private RelayCommand<CancelEventArgs> closeCommand;

        private ListCollectionView outputTypes;
        private CultureInfo[] supportedCultures;

        /// <summary>
        /// Initializes a new instance of the SettingsViewModel class.
        /// </summary>
        public SettingsViewModel()
        {
            if (this.IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                this.Settings = new Settings();
                this.Settings.ConversionPresets.Add(new ConversionPreset("Test", OutputType.Mp3));
                this.SelectedPreset = new ConversionPreset("Test", OutputType.Mp3);
            }
            else
            {
                ISettingsService settingsService = SimpleIoc.Default.GetInstance<ISettingsService>();
                this.Settings = settingsService.Settings;

                List<OutputTypeViewModel> outputTypeViewModels = new List<OutputTypeViewModel>();
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Ogg));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Mp3));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Aac));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Flac));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Wav));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Mkv));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Mp4));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Ogv));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Webm));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Avi));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Png));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Jpg));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Webp));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Ico));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Gif));
                outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Pdf));
                this.outputTypes = new ListCollectionView(outputTypeViewModels);
                this.outputTypes.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

                this.SupportedCultures = Helpers.GetSupportedCultures().ToArray();

                this.InitializeCompatibleInputExtensions();
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
        
        public InputPostConversionAction[] InputPostConversionActions => new[]
                                                                             {
                                                                                 InputPostConversionAction.None,
                                                                                 InputPostConversionAction.MoveInArchiveFolder,
                                                                                 InputPostConversionAction.Delete,
                                                                             };

        public ConversionPreset SelectedPreset
        {
            get => this.selectedPreset;

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

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.InputCategories));
                this.movePresetUpCommand?.RaiseCanExecuteChanged();
                this.movePresetDownCommand?.RaiseCanExecuteChanged();
            }
        }

        public Settings Settings
        {
            get => this.settings;

            set
            {
                this.settings = value;
                this.RaisePropertyChanged();
            }
        }

        public CultureInfo[] SupportedCultures
        {
            get => this.supportedCultures;
            set
            {
                this.supportedCultures = value;
                this.RaisePropertyChanged();
            }
        }

        public ListCollectionView OutputTypes
        {
            get => this.outputTypes;
            set
            {
                this.outputTypes = value;
                this.RaisePropertyChanged();
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

                this.RaisePropertyChanged();
            }
        }
        
        public ICommand GetChangeLogContentCommand
        {
            get
            {
                if (this.getChangeLogContentCommand == null)
                {
                    this.getChangeLogContentCommand = new RelayCommand(this.DownloadChangeLogAction);
                }

                return this.getChangeLogContentCommand;
            }
        }

        public ICommand OpenUrlCommand
        {
            get
            {
                if (this.openUrlCommand == null)
                {
                    this.openUrlCommand = new RelayCommand<string>((url) => Process.Start(url));
                }

                return this.openUrlCommand;
            }
        }

        public ICommand MovePresetUpCommand
        {
            get
            {
                if (this.movePresetUpCommand == null)
                {
                    this.movePresetUpCommand = new RelayCommand(this.MoveSelectedPresetUp, this.CanMoveSelectedPresetUp);
                }

                return this.movePresetUpCommand;
            }
        }

        public ICommand MovePresetDownCommand
        {
            get
            {
                if (this.movePresetDownCommand == null)
                {
                    this.movePresetDownCommand = new RelayCommand(this.MoveSelectedPresetDown, this.CanMoveSelectedPresetDown);
                }

                return this.movePresetDownCommand;
            }
        }

        public ICommand AddNewPresetCommand
        {
            get
            {
                if (this.addNewPresetCommand == null)
                {
                    this.addNewPresetCommand = new RelayCommand(this.AddNewPreset);
                }

                return this.addNewPresetCommand;
            }
        }

        public ICommand RemoveSelectedPresetCommand
        {
            get
            {
                if (this.removePresetCommand == null)
                {
                    this.removePresetCommand = new RelayCommand(this.RemoveSelectedPreset, this.CanRemoveSelectedPreset);
                }
                
                return this.removePresetCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (this.saveCommand == null)
                {
                    this.saveCommand = new RelayCommand(this.SaveSettings, this.CanSaveSettings);
                }

                return this.saveCommand;
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                if (this.closeCommand == null)
                {
                    this.closeCommand = new RelayCommand<CancelEventArgs>(this.CloseSettings);
                }

                return this.closeCommand;
            }
        }

        private void SelectedPresetPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == "OutputType")
            {
                this.RaisePropertyChanged(nameof(this.InputCategories));
            }

            this.saveCommand.RaiseCanExecuteChanged();
        }

        private void DownloadChangeLogAction()
        {
            IUpgradeService upgradeService = SimpleIoc.Default.GetInstance<IUpgradeService>();
            upgradeService.DownloadChangeLog();
            this.DisplaySeeChangeLogLink = false;
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
            this.RaisePropertyChanged(nameof(this.InputCategories));
        }
        
        private void CloseSettings(CancelEventArgs args)
        {
            ISettingsService settingsService = SimpleIoc.Default.GetInstance<ISettingsService>();
            settingsService.RevertSettings();

            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Settings, args != null);
        }

        private bool CanSaveSettings()
        {
            return this.settings != null && string.IsNullOrEmpty(this.settings.Error);
        }

        private void SaveSettings()
        {
            // Save changes.
            ISettingsService settingsService = SimpleIoc.Default.GetInstance<ISettingsService>();
            settingsService.Settings.Save();

            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Settings, false);
        }
        
        private bool CanMoveSelectedPresetUp()
        {
            ConversionPreset presetToMove = this.SelectedPreset;
            if (presetToMove == null)
            {
                return false;
            }

            int indexOfSelectedPreset = this.settings.ConversionPresets.IndexOf(presetToMove);
            return indexOfSelectedPreset > 0;
        }

        private void MoveSelectedPresetUp()
        {
            ConversionPreset presetToMove = this.SelectedPreset;
            if (presetToMove == null)
            {
                return;
            }

            int indexOfSelectedPreset = this.settings.ConversionPresets.IndexOf(presetToMove);
            int newIndexOfSelectedPreset = System.Math.Max(0, indexOfSelectedPreset - 1);

            this.settings.ConversionPresets.Move(indexOfSelectedPreset, newIndexOfSelectedPreset);
            this.movePresetUpCommand?.RaiseCanExecuteChanged();
            this.movePresetDownCommand?.RaiseCanExecuteChanged();
        }

        private bool CanMoveSelectedPresetDown()
        {
            ConversionPreset presetToMove = this.SelectedPreset;
            if (presetToMove == null)
            {
                return false;
            }

            int indexOfSelectedPreset = this.settings.ConversionPresets.IndexOf(presetToMove);
            return indexOfSelectedPreset < this.settings.ConversionPresets.Count - 1;
        }

        private void MoveSelectedPresetDown()
        {
            ConversionPreset presetToMove = this.SelectedPreset;
            if (presetToMove == null)
            {
                return;
            }

            int indexOfSelectedPreset = this.settings.ConversionPresets.IndexOf(presetToMove);
            int newIndexOfSelectedPreset = System.Math.Min(this.settings.ConversionPresets.Count - 1, indexOfSelectedPreset + 1);

            this.settings.ConversionPresets.Move(indexOfSelectedPreset, newIndexOfSelectedPreset);
            this.movePresetUpCommand?.RaiseCanExecuteChanged();
            this.movePresetDownCommand?.RaiseCanExecuteChanged();
        }

        private void AddNewPreset()
        {
            // Generate a unique preset name.
            ISettingsService settingsService = SimpleIoc.Default.GetInstance<ISettingsService>();
            string presetName = Properties.Resources.DefaultPresetName;
            int index = 1;
            while (settingsService.Settings.ConversionPresets.Any(match => match.Name == presetName))
            {
                index++;
                presetName = $"{Properties.Resources.DefaultPresetName} ({index})";
            }

            // Create preset by coping the selected one.
            int insertIndex = 0;
            ConversionPreset newPreset = null;
            if (this.SelectedPreset != null)
            {
                newPreset = new ConversionPreset(presetName, this.SelectedPreset);
                insertIndex = this.settings.ConversionPresets.IndexOf(this.SelectedPreset) + 1;
            }
            else
            {
                newPreset = new ConversionPreset(presetName, OutputType.Mkv, new string[0]);
                insertIndex = this.settings.ConversionPresets.Count;
            }

            settingsService.Settings.ConversionPresets.Insert(insertIndex, newPreset);
            this.SelectedPreset = newPreset;

            Messenger.Default.Send<string>("PresetName", "DoFocus");

            this.removePresetCommand.RaiseCanExecuteChanged();
        }

        private void RemoveSelectedPreset()
        {
            ISettingsService settingsService = SimpleIoc.Default.GetInstance<ISettingsService>();
            settingsService.Settings.ConversionPresets.Remove(this.selectedPreset);

            this.removePresetCommand.RaiseCanExecuteChanged();
        }

        private bool CanRemoveSelectedPreset()
        {
            return this.SelectedPreset != null;
        }
    }
}
