// <copyright file="Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Principal;
    using System.Windows;
    using System.Xml.Serialization;

    using FileConverter.Annotations;
    using Microsoft.Win32;

    [XmlRoot]
    [XmlType]
    public partial class Settings : INotifyPropertyChanged, IDataErrorInfo
    {
        private const char PresetSeparator = ';';

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlIgnore]
        public string Error
        {
            get
            {
                for (int index = 0; index < this.ConversionPresets.Count; index++)
                {
                    string error = this.ConversionPresets[index].Error;
                    if (!string.IsNullOrEmpty(error))
                    {
                        return error;
                    }
                }

                return string.Empty;
            }
        }

        private static bool IsInAdmininstratorPrivileges
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        [XmlIgnore]
        public string this[string columnName]
        {
            get
            {
                return this.Error;
            }
        }

        public static void ApplyTemporarySettings()
        {
            // Load temporary settings.
            string temporaryFilePath = Settings.GetUserSettingsTemporaryFilePath();
            if (!File.Exists(temporaryFilePath))
            {
                return;
            }

            Settings settings = null;
            XmlHelpers.LoadFromFile("Settings", temporaryFilePath, out settings);

            RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            if (registryKey == null)
            {
                Diagnostics.Debug.LogError("Can't apply settings in registry. (code 0x03)");
                return;
            }

            // Compute the registry entries data from settings.
            Dictionary<string, List<string>> registryEntries = ComputeRegistryEntriesFromConvertionPresets(settings.ConversionPresets);

            bool succeed = Settings.ApplyRegistryModifications(registryEntries);

            if (succeed)
            {
                // Copy temporary settings file to the real settings file.
                string userFilePath = Settings.GetUserSettingsFilePath();
                File.Copy(temporaryFilePath, userFilePath, true);
                File.Delete(temporaryFilePath);
            }
        }

        public static Settings Load()
        {
            Settings defaultSettings = null;

            // Load the default settings.
            string defaultFilePath = Settings.GetDefaultSettingsFilePath();
            if (File.Exists(defaultFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", defaultFilePath, out defaultSettings);
                }
                catch (Exception exception)
                {
                    Diagnostics.Debug.LogError("Fail to load file converter default settings. {0}", exception.Message);
                }
            }
            else
            {
                Diagnostics.Debug.LogError("Default settings not found at path {0}. You should try to reinstall the application.", defaultFilePath);
            }

            Settings userSettings = null;
            string userFilePath = Settings.GetUserSettingsFilePath();
            if (File.Exists(userFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", userFilePath, out userSettings);
                }
                catch (Exception)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show("Can't load file converter user settings. Do you want to fall back to default settings ?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        System.IO.File.Delete(userFilePath);
                        return Settings.Load();
                    }
                    else if (messageBoxResult == MessageBoxResult.No)
                    {
                        return null;
                    }
                }

                if (userSettings != null && userSettings.SerializationVersion != Version)
                {
                    Diagnostics.Debug.Log("File converter settings has been imported from version {0} to version {1}.", userSettings.SerializationVersion, Version);
                }
            }

            return userSettings != null ? userSettings.Merge(defaultSettings) : defaultSettings;
        }

        public ConversionPreset GetPresetFromName(string presetName)
        {
            return this.conversionPresets.FirstOrDefault(match => match.Name == presetName);
        }
        
        public void Save()
        {
            this.Clean();

            // Save the settings in a temporary files (we'll write the settings file when we'll succeed to write the registry keys).
            string temporaryFilePath = Settings.GetUserSettingsTemporaryFilePath();
            XmlHelpers.SaveToFile("Settings", temporaryFilePath, this);

            // Compute the registry entries data from settings.
            Dictionary<string, List<string>> registryEntries = Settings.ComputeRegistryEntriesFromConvertionPresets(this.ConversionPresets);

            // Detect if existing registry configuration need to be modified.
            bool registryNeedModifications = Settings.IsRegistryNeedModifications(registryEntries);

            if (registryNeedModifications)
            {
                // Write the settings in registry.
                if (Settings.IsInAdmininstratorPrivileges)
                {
                    // We are in admin mode, write reigstry ...
                    bool succeed = Settings.ApplyRegistryModifications(registryEntries);
                    if (succeed)
                    {
                        // ... and copy temporary settings file to the real settings file.
                        string userFilePath = Settings.GetUserSettingsFilePath();
                        File.Copy(temporaryFilePath, userFilePath, true);
                        File.Delete(temporaryFilePath);
                    }
                }
                else
                {
                    // Run the application in admin mode in order to perform modifications.
                    Settings.RunSaveInAdminMode();
                }
            }
            else
            {
                // Copy temporary settings file to the real settings file.
                string userFilePath = Settings.GetUserSettingsFilePath();
                File.Copy(temporaryFilePath, userFilePath, true);
                File.Delete(temporaryFilePath);
            }
        }

        public void Clean()
        {
            for (int index = 0; index < this.ConversionPresets.Count; index++)
            {
                this.ConversionPresets[index].Clean();
            }
        }

        private static bool ApplyRegistryModifications(Dictionary<string, List<string>> registryEntries)
        {
            if (!Settings.IsInAdmininstratorPrivileges)
            {
                MessageBox.Show("Can't apply settings in registry because the application is not in administrator privileges. (code 0x04)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\FileConverter", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (registryKey == null)
            {
                MessageBox.Show("Can't apply settings in registry. (code 0x05)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Can't apply settings in registry. (code 0x06)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Diagnostics.Debug.Log(exception.Message);
                }

                if (subKey == null)
                {
                    MessageBox.Show("Can't apply settings in registry. (code 0x07)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                subKey.SetValue("Presets", presets);
            }

            return true;
        }

        private static Dictionary<string, List<string>> ComputeRegistryEntriesFromConvertionPresets(ICollection<ConversionPreset> conversionPresets)
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

        private static string GetDefaultSettingsFilePath()
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

        private static string GetUserSettingsFilePath()
        {
            string path = PathHelpers.GetUserDataFolderPath();
            path = Path.Combine(path, "Settings.user.xml");
            return path;
        }

        private static string GetUserSettingsTemporaryFilePath()
        {
            string path = PathHelpers.GetUserDataFolderPath();
            path = Path.Combine(path, "Settings.temp.xml");
            return path;
        }

        private static bool IsRegistryNeedModifications(Dictionary<string, List<string>> registryEntries)
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
                    string[] presets = presetsString != null ? presetsString.Split(Settings.PresetSeparator) : new string[0];
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

        private static void RunSaveInAdminMode()
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

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Settings Merge(Settings settings)
        {
            if (settings == null)
            {
                return this;
            }

            for (int index = 0; index < settings.conversionPresets.Count; index++)
            {
                ConversionPreset conversionPreset = settings.conversionPresets[index];
                if (this.conversionPresets.Any(match => match.Name == conversionPreset.Name))
                {
                    continue;
                }

                this.conversionPresets.Add(conversionPreset);
            }

            return this;
        }
    }
}
