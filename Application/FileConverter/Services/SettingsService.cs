// <copyright file="SettingsService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using FileConverter.Properties;

namespace FileConverter.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Ioc;

    using Debug = FileConverter.Diagnostics.Debug;

    public partial class SettingsService : ObservableObject, ISettingsService
    {
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
        
        private string UserSettingsTemporaryFilePath
        {
            get
            {
                string path = FileConverterExtension.PathHelpers.GetUserDataFolderPath;
                path = Path.Combine(path, "Settings.temp.xml");
                return path;
            }
        }

        public bool PostInstallationInitialization()
        {
            Debug.Log("Execute post installation initialization.");

            Settings? defaultSettings = null;

            // Load the default settings.
            if (File.Exists(FileConverterExtension.PathHelpers.DefaultSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.DefaultSettingsFilePath, out defaultSettings);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Fail to load file converter default settings. {exception.Message}");
                    return false;
                }
            }
            else
            {
                Debug.LogError("Default settings not found at path {0}. You should try to reinstall the application.", FileConverterExtension.PathHelpers.DefaultSettingsFilePath);
                return false;
            }

            // Load user settings if exists.
            Settings? userSettings = null;
            if (File.Exists(FileConverterExtension.PathHelpers.UserSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.UserSettingsFilePath, out userSettings);
                }
                catch (Exception)
                {
                    File.Delete(FileConverterExtension.PathHelpers.UserSettingsFilePath);
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
            Settings? settings = null;
            if (File.Exists(FileConverterExtension.PathHelpers.UserSettingsFilePath))
            {
                Settings? userSettings = null;
                try
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.UserSettingsFilePath, out userSettings);
                    stopwatch.Stop();
                    Debug.Log($"Settings load time: {stopwatch.Elapsed.TotalMilliseconds}ms");

                    settings = userSettings;
                }
                catch (Exception)
                {
                    MessageBoxResult messageBoxResult =
                        MessageBox.Show(Resources.ErrorCantLoadSettings,
                            Resources.Error,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Exclamation);

                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        File.Delete(FileConverterExtension.PathHelpers.UserSettingsFilePath);
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
                if (File.Exists(FileConverterExtension.PathHelpers.DefaultSettingsFilePath))
                {
                    try
                    {
                        XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.DefaultSettingsFilePath, out Settings defaultSettings);
                        settings = defaultSettings;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("Fail to load file converter default settings. {0}", exception.Message);
                    }
                }
                else
                {
                    Debug.LogError("Default settings not found at path {0}. You should try to reinstall the application.", FileConverterExtension.PathHelpers.DefaultSettingsFilePath);
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

            // Copy temporary settings file to the real settings file.
            File.Copy(this.UserSettingsTemporaryFilePath, FileConverterExtension.PathHelpers.UserSettingsFilePath, true);
            File.Delete(this.UserSettingsTemporaryFilePath);

            return true;
        }
    }
}
