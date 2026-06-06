// <copyright file="SettingsViewModel.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ViewModels
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;

    using Microsoft.Win32;
    
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using CommunityToolkit.Mvvm.Input;

    using FileConverter.Annotations;
    using FileConverter.ConversionJobs;
    using FileConverter.Services;
    using FileConverter.Views;

    /// <summary>
    /// This class contains properties that the settings View can data bind to.
    /// </summary>
    public class SettingsViewModel : ObservableRecipient, IDataErrorInfo
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
        private RelayCommand refreshDependencyHealthCommand;
        private RelayCommand repairShellExtensionCommand;
        private RelayCommand saveCommand;
        private RelayCommand<CancelEventArgs> closeCommand;

        private ObservableCollection<DependencyStatusViewModel> dependencyStatuses = new ObservableCollection<DependencyStatusViewModel>();
        private string shellExtensionRepairStatus = string.Empty;

        private ListCollectionView outputTypes;
        private CultureInfo[] supportedCultures;
        private Helpers.HardwareAccelerationMode[] hardwareAccelerationModes = { Helpers.HardwareAccelerationMode.Off, Helpers.HardwareAccelerationMode.CUDA, Helpers.HardwareAccelerationMode.AMF };

        public event Action OnPresetCreated;
        public event Action OnFolderCreated;

        /// <summary>
        /// Initializes a new instance of the SettingsViewModel class.
        /// </summary>
        public SettingsViewModel()
        {
            this.getChangeLogContentCommand = new RelayCommand(this.DownloadChangeLogAction);
            this.openUrlCommand = new RelayCommand<string>(this.OpenUrl);
            this.createFolderCommand = new RelayCommand(this.CreateFolder);
            this.newPresetCommand = new RelayCommand(() => this.AddNewPreset(false));
            this.duplicatePresetCommand = new RelayCommand(() => this.AddNewPreset(true), this.CanDuplicateSelectedPreset);
            this.importPresetCommand = new RelayCommand(this.ImportPreset);
            this.exportPresetCommand = new RelayCommand(this.ExportSelectedPreset, this.CanExportSelectedPreset);
            this.removePresetCommand = new RelayCommand(this.RemoveSelectedPreset, this.CanRemoveSelectedPreset);
            this.refreshDependencyHealthCommand = new RelayCommand(this.RefreshDependencyHealth);
            this.repairShellExtensionCommand = new RelayCommand(this.RepairShellExtension);
            this.saveCommand = new RelayCommand(this.SaveSettings, this.CanSaveSettings);
            this.closeCommand = new RelayCommand<CancelEventArgs>(this.CloseSettings);

            ISettingsService settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
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
            outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Avif));
            outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Webp));
            outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Ico));
            outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Gif));
            outputTypeViewModels.Add(new OutputTypeViewModel(OutputType.Pdf));
            this.outputTypes = new ListCollectionView(outputTypeViewModels);
            this.outputTypes.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            this.SupportedCultures = Helpers.GetSupportedCultures().ToArray();

            this.InitializeCompatibleInputExtensions();
            this.InitializePresetFolders();
            this.RefreshDependencyHealth();
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
                this.OnPropertyChanged();
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

                this.OnPropertyChanged();
            }
        }

        public PresetFolderNode SelectedFolder
        {
            get => this.selectedFolder;

            set
            {
                this.selectedFolder = value;

                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.SelectedItem));
                this.removePresetCommand?.NotifyCanExecuteChanged();
                this.exportPresetCommand?.NotifyCanExecuteChanged();
                this.duplicatePresetCommand?.NotifyCanExecuteChanged();
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

                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.SelectedItem));
                this.OnPropertyChanged(nameof(this.InputCategories));
                this.removePresetCommand?.NotifyCanExecuteChanged();
                this.exportPresetCommand?.NotifyCanExecuteChanged();
                this.duplicatePresetCommand?.NotifyCanExecuteChanged();
            }
        }

        public Settings Settings
        {
            get => this.settings;

            set
            {
                this.settings = value;
                this.OnPropertyChanged();
            }
        }

        public CultureInfo[] SupportedCultures
        {
            get => this.supportedCultures;
            set
            {
                this.supportedCultures = value;
                this.OnPropertyChanged();
            }
        }

        public Helpers.HardwareAccelerationMode[] HardwareAccelerationModes
        {
            get => this.hardwareAccelerationModes;
            set
            {
                this.hardwareAccelerationModes = value;
                this.OnPropertyChanged();
            }
        }

        public ListCollectionView OutputTypes
        {
            get => this.outputTypes;
            set
            {
                this.outputTypes = value;
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
        
        public ICommand GetChangeLogContentCommand => this.getChangeLogContentCommand;

        public ICommand OpenUrlCommand => this.openUrlCommand;

        public ICommand CreateFolderCommand => this.createFolderCommand;

        public ICommand AddNewPresetCommand => this.newPresetCommand;

        public ICommand DuplicatePresetCommand => this.duplicatePresetCommand;

        public ICommand ImportPresetCommand => this.importPresetCommand;

        public ICommand ExportPresetCommand => this.exportPresetCommand;

        public ICommand RemoveSelectedPresetCommand => this.removePresetCommand;

        public ICommand RefreshDependencyHealthCommand => this.refreshDependencyHealthCommand;

        public ICommand RepairShellExtensionCommand => this.repairShellExtensionCommand;

        public ICommand SaveCommand => this.saveCommand;

        public ICommand CloseCommand => this.closeCommand;

        public ObservableCollection<DependencyStatusViewModel> DependencyStatuses
        {
            get => this.dependencyStatuses;

            private set
            {
                this.dependencyStatuses = value;
                this.OnPropertyChanged();
            }
        }

        public string ShellExtensionRepairStatus
        {
            get => this.shellExtensionRepairStatus;

            private set
            {
                this.shellExtensionRepairStatus = value;
                this.OnPropertyChanged();
            }
        }

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
                this.OnPropertyChanged(nameof(this.InputCategories));
            }

            this.saveCommand.NotifyCanExecuteChanged();
        }

        private void NodePropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            this.saveCommand.NotifyCanExecuteChanged();
        }

        private void DownloadChangeLogAction()
        {
            IUpgradeService upgradeService = Ioc.Default.GetRequiredService<IUpgradeService>();
            upgradeService.DownloadChangeLog();
            this.DisplaySeeChangeLogLink = false;
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            try
            {
                Process.Start(url);
            }
            catch (Exception exception)
            {
                Diagnostics.Debug.Log($"Failed to open URL '{url}': {exception.Message}.");
            }
        }

        private void RefreshDependencyHealth()
        {
            ObservableCollection<DependencyStatusViewModel> statuses = new ObservableCollection<DependencyStatusViewModel>();

            string shellExtensionPath = Helpers.GetDefaultShellExtensionPath();
            string defaultSettingsPath = FileConverterExtension.PathHelpers.DefaultSettingsFilePath;
            string userSettingsPath = FileConverterExtension.PathHelpers.UserSettingsFilePath;

            this.AddFileStatus(statuses, "FFmpeg", GetApplicationFilePath("ffmpeg.exe"), "Required for audio and video conversions.");
            this.AddFileStatus(statuses, "ImageMagick", GetApplicationFilePath("Magick.NET-Q16-AnyCPU.dll"), "Required for image, AVIF, PDF image, and WebP workflows.");
            this.AddFileStatus(statuses, "ImageMagick native", GetApplicationFilePath("Magick.Native-Q16-x64.dll"), "Required native image processing runtime.");
            this.AddFileStatus(statuses, "Ghostscript", GetApplicationFilePath("gswin64c.exe"), "Required for PDF rendering.");
            this.AddFileStatus(statuses, "Ghostscript DLL", GetApplicationFilePath("gsdll64.dll"), "Required by ImageMagick PDF rendering.");
            this.AddFileStatus(statuses, "Explorer extension DLL", shellExtensionPath, "Required for Windows Explorer right-click commands.");
            this.AddFileStatus(statuses, "Default presets", defaultSettingsPath, "Required when creating or repairing user settings.");

            if (File.Exists(userSettingsPath))
            {
                statuses.Add(new DependencyStatusViewModel("User settings", "Ready", userSettingsPath, true));
            }
            else
            {
                statuses.Add(new DependencyStatusViewModel("User settings", "Will be created", userSettingsPath, true));
            }

            this.AddOfficeStatus(statuses, "Microsoft Word", ConversionJob_Office.ApplicationName.Word, "Required for Word document conversion.");
            this.AddOfficeStatus(statuses, "Microsoft Excel", ConversionJob_Office.ApplicationName.Excel, "Required for spreadsheet conversion.");
            this.AddOfficeStatus(statuses, "Microsoft PowerPoint", ConversionJob_Office.ApplicationName.PowerPoint, "Required for presentation conversion.");
            this.AddShellRegistrationStatus(statuses);

            this.DependencyStatuses = statuses;
        }

        private void RepairShellExtension()
        {
            string shellExtensionPath = Helpers.GetDefaultShellExtensionPath();
            if (!File.Exists(shellExtensionPath))
            {
                this.ShellExtensionRepairStatus = $"Can't repair Explorer integration because {shellExtensionPath} is missing.";
                this.RefreshDependencyHealth();
                return;
            }

            string executablePath = Assembly.GetExecutingAssembly().Location;
            ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
            {
                Arguments = $"--repair-shell-extension {QuoteArgument(shellExtensionPath)}",
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                Process.Start(startInfo);
                this.ShellExtensionRepairStatus = "Repair launched with administrator privileges. Reopen Explorer or retry the context menu after it finishes.";
            }
            catch (Win32Exception exception)
            {
                if (exception.NativeErrorCode == 1223)
                {
                    this.ShellExtensionRepairStatus = "Repair canceled by user.";
                }
                else
                {
                    this.ShellExtensionRepairStatus = $"Repair failed to start: {exception.Message}";
                }
            }
            catch (Exception exception)
            {
                this.ShellExtensionRepairStatus = $"Repair failed to start: {exception.Message}";
            }

            this.RefreshDependencyHealth();
        }

        private void AddFileStatus(ObservableCollection<DependencyStatusViewModel> statuses, string name, string path, string purpose)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                statuses.Add(new DependencyStatusViewModel(name, "Ready", $"{purpose} Found at {path}", true));
            }
            else
            {
                statuses.Add(new DependencyStatusViewModel(name, "Missing", $"{purpose} Expected at {path ?? "(unknown path)"}", false));
            }
        }

        private void AddOfficeStatus(ObservableCollection<DependencyStatusViewModel> statuses, string name, ConversionJob_Office.ApplicationName applicationName, string purpose)
        {
            bool isAvailable = Helpers.IsMicrosoftOfficeApplicationAvailable(applicationName);
            statuses.Add(new DependencyStatusViewModel(
                name,
                isAvailable ? "Available" : "Optional missing",
                purpose,
                true));
        }

        private void AddShellRegistrationStatus(ObservableCollection<DependencyStatusViewModel> statuses)
        {
            string registeredPath = FileConverterExtension.PathHelpers.FileConverterPath;
            string executablePath = Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrEmpty(registeredPath))
            {
                statuses.Add(new DependencyStatusViewModel("Explorer registration", "Needs repair", "No executable path is registered in HKCU\\Software\\FileConverter.", false));
                return;
            }

            if (!File.Exists(registeredPath))
            {
                statuses.Add(new DependencyStatusViewModel("Explorer registration", "Needs repair", $"Registered executable is missing: {registeredPath}", false));
                return;
            }

            bool matchesCurrentExecutable = string.Equals(registeredPath, executablePath, StringComparison.OrdinalIgnoreCase);
            statuses.Add(new DependencyStatusViewModel(
                "Explorer registration",
                matchesCurrentExecutable ? "Ready" : "Different install",
                matchesCurrentExecutable ? registeredPath : $"Registered path: {registeredPath}; current path: {executablePath}",
                matchesCurrentExecutable));
        }

        private static string QuoteArgument(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        private static string GetApplicationFilePath(string fileName)
        {
            string applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(applicationFolder))
            {
                return fileName;
            }

            return Path.Combine(applicationFolder, fileName);
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
            this.OnPropertyChanged(nameof(this.InputCategories));
        }

        private void InitializePresetFolders()
        {
            this.presetsRootFolder = new PresetFolderNode(null, null);
            foreach (ConversionPreset preset in this.Settings.ConversionPresets)
            {
                PresetFolderNode parent = this.presetsRootFolder;
                foreach (string folderName in preset.ParentFoldersNames)
                {
                    PresetFolderNode subFolder = parent.Children.FirstOrDefault(match => match is PresetFolderNode && ((PresetFolderNode)match).Name == folderName) as PresetFolderNode;
                    if (subFolder == null)
                    {
                        subFolder = this.CreateFolderNode(folderName, parent);
                    }

                    parent = subFolder;
                }

                this.CreatePresetNode(preset, parent);
            }

            this.OnPropertyChanged(nameof(this.PresetsRootFolder));
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
            ISettingsService settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            settingsService.RevertSettings();

            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
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
            ISettingsService settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            settingsService.SaveSettings();

            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
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

            this.saveCommand.NotifyCanExecuteChanged();

            this.OnFolderCreated?.Invoke();
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
            ConversionPreset newPreset = null;
            if (this.SelectedPreset != null && duplicate)
            {
                newPreset = new ConversionPreset(presetName, this.SelectedPreset.Preset);
            }
            else
            {
                newPreset = new ConversionPreset(presetName, OutputType.Mkv, new string[0]);
            }

            PresetNode node = new PresetNode(newPreset, parent);

            parent.Children.Insert(insertIndex, node);

            node.PropertyChanged += this.NodePropertyChanged;

            this.SelectedItem = node;

            this.OnPresetCreated?.Invoke();

            this.removePresetCommand.NotifyCanExecuteChanged();
            this.saveCommand.NotifyCanExecuteChanged();
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
                    return;
                }

                string directoryPath = Path.GetDirectoryName(openFileDialog.FileName);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    this.ImportDirectoryPath = directoryPath;
                }

                List<ConversionPreset> presetsToImport = new List<ConversionPreset>();
                try
                {
                    XmlHelpers.LoadFromFile("Presets", openFileDialog.FileName, out presetsToImport);
                }
                catch (Exception exception)
                {
                    Diagnostics.Debug.LogError($"Failed to import presets. {exception.Message}");
                    return;
                }

                if (!this.ReviewImportedPresets(presetsToImport))
                {
                    return;
                }

                // Add imported preset to preset tree.
                bool itemSelected = false;
                foreach (ConversionPreset conversionPreset in presetsToImport)
                {
                    PresetFolderNode parent = this.PresetsRootFolder;
                    foreach (string folderName in conversionPreset.ParentFoldersNames)
                    {
                        PresetFolderNode folderNode = parent.Children.FirstOrDefault(match => match is PresetFolderNode && match.Name == folderName) as PresetFolderNode;
                        if (folderNode == null)
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

                this.saveCommand.NotifyCanExecuteChanged();
            }
        }

        private bool ReviewImportedPresets(List<ConversionPreset> presetsToImport)
        {
            if (presetsToImport == null || presetsToImport.Count == 0)
            {
                return true;
            }

            List<string> warnings = new List<string>();
            foreach (ConversionPreset conversionPreset in presetsToImport)
            {
                warnings.AddRange(this.GetPresetImportWarnings(conversionPreset));
            }

            if (warnings.Count == 0)
            {
                return true;
            }

            StringBuilder message = new StringBuilder();
            message.AppendLine("This preset file contains advanced settings that can affect conversion commands or output locations.");
            message.AppendLine();
            message.AppendLine("Choose Yes to import with risky settings disabled, No to import as-is only if you trust this file, or Cancel to stop importing.");
            message.AppendLine();

            int warningsToDisplay = Math.Min(warnings.Count, 8);
            for (int index = 0; index < warningsToDisplay; index++)
            {
                message.AppendLine("- " + warnings[index]);
            }

            if (warnings.Count > warningsToDisplay)
            {
                message.AppendLine($"- {warnings.Count - warningsToDisplay} more warning(s).");
            }

            MessageBoxResult result = MessageBox.Show(
                message.ToString(),
                "Review imported presets",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                foreach (ConversionPreset conversionPreset in presetsToImport)
                {
                    this.NeutralizeRiskyImportedPresetSettings(conversionPreset);
                }
            }

            return true;
        }

        private IEnumerable<string> GetPresetImportWarnings(ConversionPreset conversionPreset)
        {
            if (conversionPreset == null)
            {
                yield break;
            }

            string presetName = string.IsNullOrWhiteSpace(conversionPreset.FullName) ? "Unnamed preset" : conversionPreset.FullName;

            if (this.HasUnsafePresetName(conversionPreset))
            {
                yield return $"Preset '{presetName}' has a name that is unsafe for Explorer launch or folder creation.";
            }

            if (this.HasEnabledCustomFFmpegCommand(conversionPreset))
            {
                yield return $"Preset '{presetName}' enables a raw FFmpeg command.";
            }

            if (this.HasSuspiciousOutputTemplate(conversionPreset))
            {
                yield return $"Preset '{presetName}' writes to a non-standard output location.";
            }
        }

        private void NeutralizeRiskyImportedPresetSettings(ConversionPreset conversionPreset)
        {
            if (conversionPreset == null)
            {
                return;
            }

            if (this.HasUnsafePresetName(conversionPreset))
            {
                conversionPreset.FullName = this.SanitizePresetFullName(conversionPreset.FullName);
            }

            if (this.HasEnabledCustomFFmpegCommand(conversionPreset))
            {
                conversionPreset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.EnableFFMPEGCustomCommand, "False");
                conversionPreset.SetSettingsValue(ConversionPreset.ConversionSettingKeys.FFMPEGCustomCommand, string.Empty);
            }

            if (this.HasSuspiciousOutputTemplate(conversionPreset))
            {
                conversionPreset.OutputFileNameTemplate = "(p)(f)";
            }
        }

        private bool HasEnabledCustomFFmpegCommand(ConversionPreset conversionPreset)
        {
            bool customCommandEnabled;
            bool.TryParse(
                conversionPreset.GetSettingsValue(ConversionPreset.ConversionSettingKeys.EnableFFMPEGCustomCommand),
                out customCommandEnabled);

            return customCommandEnabled &&
                !string.IsNullOrWhiteSpace(conversionPreset.GetSettingsValue(ConversionPreset.ConversionSettingKeys.FFMPEGCustomCommand));
        }

        private bool HasSuspiciousOutputTemplate(ConversionPreset conversionPreset)
        {
            string template = conversionPreset.OutputFileNameTemplate;
            if (string.IsNullOrWhiteSpace(template))
            {
                return false;
            }

            if (Path.IsPathRooted(template))
            {
                return true;
            }

            string[] segments = template.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < segments.Length; index++)
            {
                if (segments[index] == "." || segments[index] == "..")
                {
                    return true;
                }
            }

            string lowerTemplate = template.ToLowerInvariant();
            return
                lowerTemplate.Contains("(p:d)") ||
                lowerTemplate.Contains("(p:documents)") ||
                lowerTemplate.Contains("(p:m)") ||
                lowerTemplate.Contains("(p:music)") ||
                lowerTemplate.Contains("(p:v)") ||
                lowerTemplate.Contains("(p:videos)") ||
                lowerTemplate.Contains("(p:p)") ||
                lowerTemplate.Contains("(p:pictures)");
        }

        private bool HasUnsafePresetName(ConversionPreset conversionPreset)
        {
            return this.SanitizePresetFullName(conversionPreset.FullName) != conversionPreset.FullName;
        }

        private string SanitizePresetFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Imported preset";
            }

            string[] segments = fullName.Split('/');
            for (int index = 0; index < segments.Length; index++)
            {
                segments[index] = this.SanitizePresetNameSegment(segments[index], index == segments.Length - 1);
            }

            return string.Join("/", segments);
        }

        private string SanitizePresetNameSegment(string segment, bool isPresetName)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return isPresetName ? "Imported preset" : "Imported";
            }

            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(segment.Length);
            for (int index = 0; index < segment.Length; index++)
            {
                char character = segment[index];
                if (char.IsControl(character) ||
                    character == '"' ||
                    character == '/' ||
                    character == '\\' ||
                    Array.IndexOf(invalidFileNameChars, character) >= 0)
                {
                    builder.Append('_');
                    continue;
                }

                builder.Append(character);
            }

            string sanitizedSegment = builder.ToString().Trim();
            if (string.IsNullOrEmpty(sanitizedSegment) ||
                sanitizedSegment == "." ||
                sanitizedSegment == "..")
            {
                return isPresetName ? "Imported preset" : "Imported";
            }

            if (sanitizedSegment.StartsWith("-", StringComparison.Ordinal))
            {
                sanitizedSegment = "_" + sanitizedSegment;
            }

            return sanitizedSegment;
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

            this.removePresetCommand.NotifyCanExecuteChanged();
            this.saveCommand.NotifyCanExecuteChanged();
        }

        private bool CanRemoveSelectedPreset()
        {
            return this.SelectedItem != null && this.SelectedItem.Parent != null;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

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
