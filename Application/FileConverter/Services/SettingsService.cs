// <copyright file="SettingsService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using FileConverter.Diagnostics;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Ioc;

    public class SettingsService : ObservableObject, ISettingsService
    {
        public SettingsService()
        {
            // Load settigns.
            Debug.Log("Load settings...");
            this.Settings = Settings.Load();

            SimpleIoc.Default.Register<ISettingsService>(() => this);
        }

        public Settings Settings
        {
            get;
            private set;
        }

        public void RevertSettings()
        {
            // Load previous preset in order to cancel changes.
            this.Settings = Settings.Load();
        }
    }
}
