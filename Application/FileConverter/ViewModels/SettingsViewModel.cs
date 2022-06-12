// <copyright file="SettingsViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;

    using FileConverter.Annotations;
    using FileConverter.Services;
    using FileConverter.Views;

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
    public class SettingsViewModel : ViewModelBase, IDataErrorInfo
    {
        private InputExtensionCategory[] inputCategories;
        private PresetFolderNode presetsRootFolder;
        private PresetFolderNode selectedFolder;
        private PresetNode selectedPreset;
        private Settings settings;
        private bool displaySeeChangeLogLink = true;

        private RelayCommand<string> openUrlCommand;
        private RelayCommand getChangeLogContentCommand;
        private RelayCommand createFolderCommand;
        private RelayCommand newPresetCommand;
        private RelayCommand duplicatePresetCommand;
        private RelayCommand importPresetCommand;
        private RelayCommand exportPresetCommand;
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
            this.getChangeLogContentCommand = new RelayCommand(this.DownloadChangeLogAction);
            this.openUrlCommand = new RelayCommand<string>((url) => Process.Start(url));
            this.createFolderCommand = new RelayCommand(this.CreateFolder);
            this.newPresetCommand = new RelayCommand(() => this.AddNewPreset(false));
            this.duplicatePresetCommand = new RelayCommand(() => this.AddNewPreset(true), this.CanDuplicateSelectedPreset);
            this.importPresetCommand = new RelayCommand(this.ImportPreset);
            this.exportPresetCommand = new RelayCommand(this.ExportSelectedPreset, this.CanExportSelectedPreset);
            this.removePresetCommand = new RelayCommand(this.RemoveSelectedPreset, this.CanRemoveSelectedPreset);
            this.saveCommand = new RelayCommand(this.SaveSettings, this.CanSaveSettings);
            this.closeCommand = new RelayCommand<CancelEventArgs>(this.CloseSettings);

            if (this.IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                this.Settings = new Settings();
                this.Settings.ConversionPresets.Add(new ConversionPreset("Test", OutputType.Mp3));
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
                this.InitializePresetFolders();
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
                    if (this.SelectedPreset == null || Helpers.IsOutputTypeCompatibleWithCategory(this.SelectedPreset.Preset.OutputType, category.Name))
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

        public PresetFolderNode PresetsRootFolder
        {
            get => this.presetsRootFolder;

            set
            {
                this.presetsRootFolder = value;
                this.RaisePropertyChanged();
            }
        }

        public AbstractTreeNode SelectedItem
        {
            get
            {
                if (this.SelectedFolder != null)
                {
                    return this.SelectedFolder;
                }

                return this.SelectedPreset;
            }

            set
            {
                if (value is PresetNode preset)
                {
                    this.SelectedPreset = preset;
                    this.SelectedFolder = null;
                }
                else if (value is PresetFolderNode folder)
                {
                    this.SelectedFolder = folder;
                    this.SelectedPreset = null;
                }
                else
                {
                    this.SelectedPreset = null;
                    this.SelectedFolder = null;
                }

                this.RaisePropertyChanged();
            }
        }

        public PresetFolderNode SelectedFolder
        {
            get => this.selectedFolder;

            set
            {
                this.selectedFolder = value;

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.SelectedItem));
                this.removePresetCommand?.RaiseCanExecuteChanged();
                this.exportPresetCommand?.RaiseCanExecuteChanged();
                this.duplicatePresetCommand?.RaiseCanExecuteChanged();
            }
        }

        public PresetNode SelectedPreset
        {
            get => this.selectedPreset;

            set
            {
                if (this.selectedPreset != null)
                {
                    this.selectedPreset.Preset.PropertyChanged -= this.SelectedPresetPropertyChanged;
                }

                this.selectedPreset = value;

                if (this.selectedPreset != null)
                {
                    this.selectedPreset.Preset.PropertyChanged += this.SelectedPresetPropertyChanged;
                }

                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.SelectedItem));
                this.RaisePropertyChanged(nameof(this.InputCategories));
                this.removePresetCommand?.RaiseCanExecuteChanged();
                this.exportPresetCommand?.RaiseCanExecuteChanged();
                this.duplicatePresetCommand?.RaiseCanExecuteChanged();
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
        
        public ICommand GetChangeLogContentCommand => this.getChangeLogContentCommand;

        public ICommand OpenUrlCommand => this.openUrlCommand;

        public ICommand CreateFolderCommand => this.createFolderCommand;

        public ICommand AddNewPresetCommand => this.newPresetCommand;

        public ICommand DuplicatePresetCommand => this.duplicatePresetCommand;

        public ICommand ImportPresetCommand => this.importPresetCommand;

        public ICommand ExportPresetCommand => this.exportPresetCommand;

        public ICommand RemoveSelectedPresetCommand => this.removePresetCommand;

        public ICommand SaveCommand => this.saveCommand;

        public ICommand CloseCommand => this.closeCommand;

        public TreeViewSelectionBehavior.IsChildOfPredicate PresetsHierarchyPredicate => (object nodeA, object nodeB) =>
            {
                if (nodeA is PresetNode)
                {
                    return false;
                }

                PresetFolderNode parentFolder = nodeA as PresetFolderNode;
                Diagnostics.Debug.Assert(parentFolder != null, "Node should be a preset folder.");

                return parentFolder.IsNodeInHierarchy(nodeB as AbstractTreeNode, true);
            };

        public string Error
        {
            get
            {
                string nodeError = this.CheckErrorRecursively(this.presetsRootFolder);
                if (!string.IsNullOrEmpty(nodeError))
                {
                    return nodeError;
                }

                return string.Empty;
            }
        }

        public string this[string columnName] => this.Error;

        [NotNull]
        public string ImportDirectoryPath
        {
            get
            {
                string path = FileConverter.Registry.GetValue(FileConverter.Registry.Keys.ImportInitialFolder, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                if (!Directory.Exists(path))
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                return path;
            }

            set
            {
                if (!Directory.Exists(value))
                {
                    return;
                }

                FileConverter.Registry.SetValue(FileConverter.Registry.Keys.ImportInitialFolder, value);
            }
        }

        private string CheckErrorRecursively(AbstractTreeNode node)
        {
            string nodeError = node.Error;
            if (!string.IsNullOrEmpty(nodeError))
            {
                return nodeError;
            }

            if (node is PresetFolderNode folder)
            {
                foreach (AbstractTreeNode child in folder.Children)
                {
                    nodeError = this.CheckErrorRecursively(child);
                    if (!string.IsNullOrEmpty(nodeError))
                    {
                        return nodeError;
                    }
                }
            }

            return string.Empty;
        }

        private void SelectedPresetPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == "OutputType")
            {
                this.RaisePropertyChanged(nameof(this.InputCategories));
            }

            this.saveCommand.RaiseCanExecuteChanged();
        }

        private void NodePropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
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
            List<InputExtensionCategory> categories = new List<InputExtensionCategory>();
            for (int index = 0; index < Helpers.CompatibleInputExtensions.Length; index++)
            {
                string compatibleInputExtension = Helpers.CompatibleInputExtensions[index];
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

        private void InitializePresetFolders()
        {
            this.presetsRootFolder = new PresetFolderNode(null, null);
            foreach (ConversionPreset preset in this.Settings.ConversionPresets)
            {
                PresetFolderNode parent = this.presetsRootFolder;
                foreach (string folderName in preset.ParentFoldersNames)
                {
                    if (parent.Children.FirstOrDefault(match => match is PresetFolderNode && ((PresetFolderNode)match).Name == folderName) is not PresetFolderNode subFolder)
                    {
                        subFolder = this.CreateFolderNode(folderName, parent);
                    }

                    parent = subFolder;
                }

                this.CreatePresetNode(preset, parent);
            }

            this.RaisePropertyChanged(nameof(this.PresetsRootFolder));
        }

        private void ComputePresetsParentFoldersNamesAndFillSettings(AbstractTreeNode node, List<string> folderNamesCache)
        {
            if (node is PresetFolderNode folder)
            {
                if (!string.IsNullOrEmpty(folder.Name))
                {
                    folderNamesCache.Add(folder.Name);
                }

                foreach (var child in folder.Children)
                {
                    this.ComputePresetsParentFoldersNamesAndFillSettings(child, folderNamesCache);
                }

                if (!string.IsNullOrEmpty(folder.Name))
                {
                    folderNamesCache.RemoveAt(folderNamesCache.Count - 1);
                }
            }
            else if (node is PresetNode preset)
            {
                preset.Preset.ParentFoldersNames = folderNamesCache.ToArray();
                this.settings.ConversionPresets.Add(preset.Preset);
            }
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
            return string.IsNullOrEmpty(this.Error);
        }

        private void SaveSettings()
        {
            // Compute parent folder names.
            this.settings.ConversionPresets.Clear();
            this.ComputePresetsParentFoldersNamesAndFillSettings(this.presetsRootFolder, new List<string>());
            
            // Save changes.
            ISettingsService settingsService = SimpleIoc.Default.GetInstance<ISettingsService>();
            settingsService.SaveSettings();

            INavigationService navigationService = SimpleIoc.Default.GetInstance<INavigationService>();
            navigationService.Close(Pages.Settings, false);
        }

        private void CreateFolder()
        {
            PresetFolderNode parent;
            if (this.SelectedFolder != null)
            {
                parent = this.SelectedFolder;
            }
            else if (this.SelectedItem != null)
            {
                parent = this.SelectedItem.Parent;
            }
            else
            {
                parent = this.presetsRootFolder;
            }

            int insertIndex = parent.Children.IndexOf(this.SelectedItem) + 1;
            if (insertIndex < 0)
            {
                insertIndex = parent.Children.Count;
            }

            // Generate a unique folder name.
            string folderName = Properties.Resources.DefaultFolderName;
            int index = 1;
            while (parent.Children.Any(match => match is PresetFolderNode folder && folder.Name == folderName))
            {
                index++;
                folderName = $"{Properties.Resources.DefaultFolderName} ({index})";
            }

            PresetFolderNode newFolder = new PresetFolderNode(folderName, parent);

            parent.Children.Insert(insertIndex, newFolder);

            newFolder.PropertyChanged += this.NodePropertyChanged;

            this.SelectedItem = newFolder;

            this.saveCommand.RaiseCanExecuteChanged();

            Messenger.Default.Send<string>("FolderName", "DoFocus");
        }

        private bool CanDuplicateSelectedPreset()
        {
            return this.SelectedPreset != null;
        }

        private void AddNewPreset(bool duplicate)
        {
            PresetFolderNode parent;
            if (this.SelectedFolder != null)
            {
                parent = this.SelectedFolder;
            }
            else if (this.SelectedItem != null)
            {
                parent = this.SelectedItem.Parent;
            }
            else
            {
                parent = this.presetsRootFolder;
            }

            int insertIndex = parent.Children.IndexOf(this.SelectedItem) + 1;
            if (insertIndex < 0)
            {
                insertIndex = parent.Children.Count;
            }

            // Generate a unique preset name.
            string presetName = Properties.Resources.DefaultPresetName;
            int index = 1;
            while (parent.Children.Any(match => match is PresetNode folder && folder.Preset.ShortName == presetName))
            {
                index++;
                presetName = $"{Properties.Resources.DefaultPresetName} ({index})";
            }

            // Create preset by copying the selected one.
            ConversionPreset? newPreset = null;
            if (this.SelectedPreset != null && duplicate)
            {
                newPreset = new ConversionPreset(presetName, this.SelectedPreset.Preset);
            }
            else
            {
                newPreset = new ConversionPreset(presetName, OutputType.Mkv, Array.Empty<string>());
            }

            PresetNode node = new PresetNode(newPreset, parent);

            parent.Children.Insert(insertIndex, node);

            node.PropertyChanged += this.NodePropertyChanged;

            this.SelectedItem = node;

            Messenger.Default.Send<string>("PresetName", "DoFocus");

            this.removePresetCommand.RaiseCanExecuteChanged();
            this.saveCommand.RaiseCanExecuteChanged();
        }

        private void ImportPreset()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Import presets",
                Filter = "Preset file (*.xml)|*.xml",
                InitialDirectory = this.ImportDirectoryPath,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (!File.Exists(openFileDialog.FileName))
                {
                    Diagnostics.Debug.LogError("File does not exists.");
                }

                string directoryPath = Path.GetDirectoryName(openFileDialog.FileName);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    this.ImportDirectoryPath = directoryPath;
                }

                List<ConversionPreset> presetsToImport = new List<ConversionPreset>();
                XmlHelpers.LoadFromFile("Presets", openFileDialog.FileName, out presetsToImport);

                // Add imported preset to preset tree.
                bool itemSelected = false;
                foreach (ConversionPreset conversionPreset in presetsToImport)
                {
                    PresetFolderNode parent = this.PresetsRootFolder;
                    foreach (string folderName in conversionPreset.ParentFoldersNames)
                    {
                        if (parent.Children.FirstOrDefault(match => match is PresetFolderNode && match.Name == folderName) is not PresetFolderNode folderNode)
                        {
                            folderNode = this.CreateFolderNode(folderName, parent);

                            if (!itemSelected)
                            {
                                this.SelectedItem = folderNode;
                                itemSelected = true;
                            }
                        }

                        parent = folderNode;
                    }

                    PresetNode node = this.CreatePresetNode(conversionPreset, parent);
                    if (!itemSelected)
                    {
                        this.SelectedItem = node;
                        itemSelected = true;
                    }
                }
            }
        }

        private void ExportSelectedPreset()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Export selected preset or folder",
                Filter = "Preset file (*.xml)|*.xml",
                InitialDirectory = this.ImportDirectoryPath,
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    this.ImportDirectoryPath = directoryPath;
                }

                if (Path.GetExtension(filePath) != ".xml")
                {
                    filePath += ".xml";
                }

                this.settings.ConversionPresets.Clear();
                this.ComputePresetsParentFoldersNamesAndFillSettings(this.presetsRootFolder, new List<string>());

                List<ConversionPreset> presetsToExport = new List<ConversionPreset>();
                this.FillWithPresetsRecursively(this.SelectedItem, presetsToExport);

                XmlHelpers.SaveToFile("Presets", filePath, presetsToExport);
            }
        }

        private bool CanExportSelectedPreset()
        {
            return this.SelectedItem != null;
        }

        private void RemoveSelectedPreset()
        {
            this.SelectedItem.PropertyChanged -= this.NodePropertyChanged;

            this.SelectedItem.Parent.Children.Remove(this.SelectedItem);

            this.SelectedItem = null;

            this.removePresetCommand.RaiseCanExecuteChanged();
            this.saveCommand.RaiseCanExecuteChanged();
        }

        private bool CanRemoveSelectedPreset()
        {
            return this.SelectedItem != null;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            this.UnbindNode(this.presetsRootFolder);
        }

        private void UnbindNode(AbstractTreeNode node)
        {
            node.PropertyChanged -= this.NodePropertyChanged;

            if (node is PresetFolderNode folder)
            {
                foreach (AbstractTreeNode child in folder.Children)
                {
                    this.UnbindNode(child);
                }
            }
        }

        private void FillWithPresetsRecursively(AbstractTreeNode node, List<ConversionPreset> presets)
        {
            if (node is PresetNode presetNode)
            {
                presets.Add(presetNode.Preset);
            }
            else if (node is PresetFolderNode folder)
            {
                foreach (AbstractTreeNode childNode in folder.Children)
                {
                    this.FillWithPresetsRecursively(childNode, presets);
                }
            }
        }

        private PresetFolderNode CreateFolderNode(string folderName, PresetFolderNode parent)
        {
            PresetFolderNode subFolder = new PresetFolderNode(folderName, parent);
            parent.Children.Add(subFolder);

            subFolder.PropertyChanged += this.NodePropertyChanged;
            return subFolder;
        }

        private PresetNode CreatePresetNode(ConversionPreset preset, PresetFolderNode parent)
        {
            PresetNode presetNode = new PresetNode(preset, parent);
            parent.Children.Add(presetNode);

            presetNode.PropertyChanged += this.NodePropertyChanged;
            return presetNode;
        }
    }
}
