// <copyright file="ISettingsService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    public interface ISettingsService
    {
        Settings Settings
        {
            get;
        }

        bool PostInstallationInitialization();

        void SaveSettings();

        void RevertSettings();
    }
}
