// <copyright file="Settings.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Security.Principal;
    using System.Windows;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Microsoft.Win32;

    using FileConverter.Annotations;

    public class Settings : INotifyPropertyChanged
    {
        private const char PresetSeparator = ';';

        private ObservableCollection<ConversionPreset> conversionPresets = new ObservableCollection<ConversionPreset>();

        public event PropertyChangedEventHandler PropertyChanged;

        private static bool IsInAdmininstratorPrivileges
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public ObservableCollection<ConversionPreset> ConversionPresets
        {
            get { return this.conversionPresets; }
            set { this.conversionPresets = value; }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ConversionPreset GetPresetFromName(string presetName)
        {
            return this.conversionPresets.FirstOrDefault(match => match.Name == presetName);
        }

        public void Load()
        {
            ICollection<ConversionPreset> presets = this.ConversionPresets;
            presets.Clear();

            string userFilePath = Settings.GetUserSettingsFilePath();
            if (File.Exists(userFilePath))
            {
                XmlHelpers.LoadFromFile<ConversionPreset>("Settings", userFilePath, ref presets);
            }
            else
            {
                // If user settings doesn't exist, load the default settings.
                string defaultFilePath = Settings.GetDefaultSettingsFilePath();
                if (File.Exists(defaultFilePath))
                {
                    XmlHelpers.LoadFromFile<ConversionPreset>("Settings", defaultFilePath, ref presets);
                }
                else
                {
                    Diagnostics.Log("Default settings not found. You should try to reinstall the application.");
                }
            }
        }

        public void Save()
        {
            // Save the settings in a temporary files (we'll write the settings file when we'll succeed to write the registry keys).
            string temporaryFilePath = Settings.GetUserSettingsTemporaryFilePath();
            XmlHelpers.SaveToFile("Settings", temporaryFilePath, this.ConversionPresets);

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

        public static void ApplyTemporarySettings()
        {
            // Load temporary settings.
            string temporaryFilePath = Settings.GetUserSettingsTemporaryFilePath();
            if (!File.Exists(temporaryFilePath))
            {
                return;
            }

            ICollection<ConversionPreset> conversionPresets = new ObservableCollection<ConversionPreset>();
            XmlHelpers.LoadFromFile("Settings", temporaryFilePath, ref conversionPresets);

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
            if (registryKey == null)
            {
                MessageBox.Show("Can't apply settings in registry.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Compute the registry entries data from settings.
            Dictionary<string, List<string>> registryEntries = ComputeRegistryEntriesFromConvertionPresets(conversionPresets);

            bool succeed = Settings.ApplyRegistryModifications(registryEntries);

            if (succeed)
            {
                // Copy temporary settings file to the real settings file.
                string userFilePath = Settings.GetUserSettingsFilePath();
                File.Copy(temporaryFilePath, userFilePath, true);
                File.Delete(temporaryFilePath);
            }
        }

        private static bool ApplyRegistryModifications(Dictionary<string, List<string>> registryEntries)
        {
            if (!Settings.IsInAdmininstratorPrivileges)
            {
                MessageBox.Show("Can't apply settings in registry because the application is not in administrator privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (registryKey == null)
            {
                MessageBox.Show("Can't apply settings in registry (ErrorCode=1).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                catch (Exception exception)
                {
                    MessageBox.Show("Can't apply settings in registry (ErrorCode=2).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Diagnostics.Log(exception.Message);
                }

                if (subKey == null)
                {
                    MessageBox.Show("Can't apply settings in registry (ErrorCode=3).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            path = Path.Combine(path, "Settings.xml");
            return path;
        }

        private static string GetUserSettingsFilePath()
        {
            string path = Environment.GetEnvironmentVariable("LocalAppData");
            path = Path.Combine(path, "FileConverter");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "Settings.user.xml");
            return path;
        }

        private static string GetUserSettingsTemporaryFilePath()
        {
            string path = Environment.GetEnvironmentVariable("LocalAppData");
            path = Path.Combine(path, "FileConverter");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, "Settings.temp.xml");
            return path;
        }

        private static bool IsRegistryNeedModifications(Dictionary<string, List<string>> registryEntries)
        {
            // Compare to registry entries.
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\FileConverter");
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
                        if (!presetsList.Contains(presets[presetIndex]))
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
            catch (Exception exception)
            {
                MessageBox.Show("Can't apply settings in registry because the application has no administrator privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    }
}
