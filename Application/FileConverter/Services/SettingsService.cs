// <copyright file="SettingsService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Ioc;

    using Microsoft.Win32;

    using Debug = FileConverter.Diagnostics.Debug;

    public partial class SettingsService : ObservableObject, ISettingsService
    {
        private const char PresetSeparator = ';';

        public SettingsService()
        {
            // Load settigns.
            Debug.Log("Load settings...");
            this.Settings = this.Load();

            SimpleIoc.Default.Register<ISettingsService>(() => this);
        }

        public Settings Settings
        {
            get;
            private set;
        }

        private string DefaultSettingsFilePath
        {
            get
            {
                string path = Assembly.GetEntryAssembly().Location;
                path = Path.GetDirectoryName(path);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = Path.Combine(path, "Settings.default.xml");
                return path;
            }
        }

        private string UserSettingsFilePath
        {
            get
            {
                string path = PathHelpers.GetUserDataFolderPath();
                path = Path.Combine(path, "Settings.user.xml");
                return path;
            }
        }

        private string UserSettingsTemporaryFilePath
        {
            get
            {
                string path = PathHelpers.GetUserDataFolderPath();
                path = Path.Combine(path, "Settings.temp.xml");
                return path;
            }
        }

        public bool PostInstallationInitialization()
        {
            Debug.Log("Execute post installation initialization.");

            Settings defaultSettings = null;

            // Load the default settings.
            if (File.Exists(this.DefaultSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", this.DefaultSettingsFilePath, out defaultSettings);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Fail to load file converter default settings. {exception.Message}");
                    return false;
                }
            }
            else
            {
                Debug.LogError("Default settings not found at path {0}. You should try to reinstall the application.", this.DefaultSettingsFilePath);
                return false;
            }

            // Load user settings if exists.
            Settings userSettings = null;
            if (File.Exists(this.UserSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", this.UserSettingsFilePath, out userSettings);
                }
                catch (Exception)
                {
                    File.Delete(this.UserSettingsFilePath);
                }

                if (userSettings != null)
                {
                    if (userSettings.SerializationVersion != Settings.Version)
                    {
                        this.MigrateSettingsToCurrentVersion(userSettings);

                        Debug.Log("File converter settings have been imported from version {0} to version {1}.", userSettings.SerializationVersion, Settings.Version);
                        userSettings.SerializationVersion = Settings.Version;
                    }

                    // Remove default settings.
                    if (userSettings.ConversionPresets != null)
                    {
                        for (int index = userSettings.ConversionPresets.Count - 1; index >= 0; index--)
                        {
                            if (userSettings.ConversionPresets[index].IsDefaultSettings)
                            {
                                userSettings.ConversionPresets.RemoveAt(index);
                            }
                        }
                    }
                }
            }

            Settings settings = userSettings != null ? userSettings.Merge(defaultSettings) : defaultSettings;
            return this.Save(settings);
        }

        public void ApplyTemporarySettings()
        {
            // Load temporary settings.
            if (!File.Exists(this.UserSettingsTemporaryFilePath))
            {
                return;
            }

            Settings settings = null;
            XmlHelpers.LoadFromFile("Settings", this.UserSettingsTemporaryFilePath, out settings);

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            if (registryKey == null)
            {
                Debug.LogError(0x03, "Can't apply settings in registry.");
                return;
            }

            // Compute the registry entries data from settings.
            Dictionary<string, List<string>> registryEntries = ComputeRegistryEntriesFromConversionPresets(settings.ConversionPresets);

            bool succeed = this.ApplyRegistryModifications(registryEntries);

            if (succeed)
            {
                // Copy temporary settings file to the real settings file.
                File.Copy(this.UserSettingsTemporaryFilePath, this.UserSettingsFilePath, true);
                File.Delete(this.UserSettingsTemporaryFilePath);
            }
        }

        public void SaveSettings()
        {
            this.Save(this.Settings);
        }

        public void RevertSettings()
        {
            // Load previous preset in order to cancel changes.
            this.Settings = this.Load();
        }

        private Settings Load()
        {
            Settings settings = null;
            if (File.Exists(this.UserSettingsFilePath))
            {
                Settings userSettings = null;
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", this.UserSettingsFilePath, out userSettings);
                    settings = userSettings;
                }
                catch (Exception)
                {
                    MessageBoxResult messageBoxResult =
                        MessageBox.Show(
                            "Can't load file converter user settings. Do you want to fall back to default settings ?",
                            "Error",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Exclamation);

                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        File.Delete(this.UserSettingsFilePath);
                        return this.Load();
                    }
                    else if (messageBoxResult == MessageBoxResult.No)
                    {
                        return null;
                    }
                }

                if (userSettings != null && userSettings.SerializationVersion != Settings.Version)
                {
                    this.MigrateSettingsToCurrentVersion(userSettings);

                    Diagnostics.Debug.Log("File converter settings has been imported from version {0} to version {1}.", userSettings.SerializationVersion, Settings.Version);
                    userSettings.SerializationVersion = Settings.Version;
                    this.Save(userSettings);
                }
            }
            else
            {
                // Load the default settings.
                if (File.Exists(this.DefaultSettingsFilePath))
                {
                    Settings defaultSettings = null;
                    try
                    {
                        XmlHelpers.LoadFromFile<Settings>("Settings", this.DefaultSettingsFilePath, out defaultSettings);
                        settings = defaultSettings;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("Fail to load file converter default settings. {0}", exception.Message);
                    }
                }
                else
                {
                    Debug.LogError("Default settings not found at path {0}. You should try to reinstall the application.", this.DefaultSettingsFilePath);
                }
            }

            return settings;
        }

        private bool Save(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Clean();

            // Save the settings in a temporary files (we'll write the settings file when we'll succeed to write the registry keys).
            XmlHelpers.SaveToFile("Settings", this.UserSettingsTemporaryFilePath, settings);

            // Compute the registry entries data from settings.
            Dictionary<string, List<string>> registryEntries = ComputeRegistryEntriesFromConversionPresets(settings.ConversionPresets);

            // Detect if existing registry configuration need to be modified.
            bool registryNeedModifications = this.DoesRegistryNeedModifications(registryEntries);

            if (registryNeedModifications)
            {
                // Write the settings in registry.
                bool succeed = this.ApplyRegistryModifications(registryEntries);
                if (succeed)
                {
                    // ... and copy temporary settings file to the real settings file.
                    File.Copy(this.UserSettingsTemporaryFilePath, this.UserSettingsFilePath, true);
                    File.Delete(this.UserSettingsTemporaryFilePath);
                }
                else
                {
                    if (!FileConverter.Application.IsInAdmininstratorPrivileges)
                    {
                        // Run the application in admin mode in order to perform modifications.
                        Debug.Log("Can't apply registry modifications, fallback to admin privileges.");
                        this.RunSaveInAdminMode();
                    }
                    else
                    {
                        Debug.LogError(0x09, "Can't apply settings in registry.");
                        return false;
                    }
                }
            }
            else
            {
                // Copy temporary settings file to the real settings file.
                File.Copy(this.UserSettingsTemporaryFilePath, this.UserSettingsFilePath, true);
                File.Delete(this.UserSettingsTemporaryFilePath);
            }

            return true;
        }

        private void RunSaveInAdminMode()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location, "--apply-settings")
                                                    {
                                                        UseShellExecute = true,
                                                        Verb = "runas", // indicates to elevate privileges
                                                    };

            Process process = new Process
                                  {
                                      EnableRaisingEvents = true, // enable WaitForExit()
                                      StartInfo = processStartInfo
                                  };

            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch (Exception)
            {
                MessageBox.Show("Can't apply settings in registry because the application has no administrator privileges. (code 0x08)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private bool DoesRegistryNeedModifications(Dictionary<string, List<string>> registryEntries)
        {
            // Compare to registry entries.
            RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            if (registryKey == null)
            {
                return true;
            }

            string[] subKeyNames = registryKey.GetSubKeyNames();
            if (subKeyNames.Length == registryEntries.Count)
            {
                for (int index = 0; index < subKeyNames.Length; index++)
                {
                    string subKeyName = subKeyNames[index];
                    if (!registryEntries.ContainsKey(subKeyName))
                    {
                        return true;
                    }

                    List<string> presetsList = registryEntries[subKeyName];

                    RegistryKey subKey = registryKey.OpenSubKey(subKeyName);
                    if (subKey == null)
                    {
                        return true;
                    }

                    string presetsString = subKey.GetValue("Presets", string.Empty) as string;
                    string[] presets = presetsString != null ? presetsString.Split(PresetSeparator) : new string[0];
                    if (presets.Length != presetsList.Count)
                    {
                        return true;
                    }

                    for (int presetIndex = 0; presetIndex < presets.Length; presetIndex++)
                    {
                        if (presetsList[presetIndex] != presets[presetIndex])
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        private bool ApplyRegistryModifications(Dictionary<string, List<string>> registryEntries)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (registryKey == null)
            {
                Debug.LogError(0x05, "Can't find File Converter registry entry.");
                return false;
            }

            // Delete previous settings.
            string[] subKeyNames = registryKey.GetSubKeyNames();
            for (int index = 0; index < subKeyNames.Length; index++)
            {
                try
                {
                    registryKey.DeleteSubKey(subKeyNames[index]);
                }
                catch (Exception)
                {
                    Debug.LogError(0x06, "Can't apply settings in registry.");
                    return false;
                }
            }

            // Apply settings in registry.
            foreach (KeyValuePair<string, List<string>> registryEntry in registryEntries)
            {
                string presets = string.Empty;
                for (int index = 0; index < registryEntry.Value.Count; index++)
                {
                    if (index > 0)
                    {
                        presets += ";";
                    }

                    presets += registryEntry.Value[index];
                }

                RegistryKey subKey = null;
                try
                {
                    subKey = registryKey.CreateSubKey(registryEntry.Key);
                }
                catch (Exception exception)
                {
                    Debug.Log(exception.Message);
                }

                if (subKey == null)
                {
                    Debug.LogError(0x07, "Can't apply settings in registry.");
                    return false;
                }

                subKey.SetValue("Presets", presets);
            }

            return true;
        }

        private static Dictionary<string, List<string>> ComputeRegistryEntriesFromConversionPresets(ICollection<ConversionPreset> conversionPresets)
        {
            Dictionary<string, List<string>> registryEntries = new Dictionary<string, List<string>>();

            foreach (ConversionPreset conversionPreset in conversionPresets)
            {
                List<string> inputTypes = conversionPreset.InputTypes;
                for (int inputIndex = 0; inputIndex < inputTypes.Count; inputIndex++)
                {
                    string inputType = inputTypes[inputIndex].ToLowerInvariant();
                    if (!registryEntries.ContainsKey(inputType))
                    {
                        registryEntries.Add(inputType, new List<string>());
                    }

                    registryEntries[inputType].Add(conversionPreset.Name);
                }
            }

            return registryEntries;
        }

    }
}
