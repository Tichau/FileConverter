// <copyright file="IUpgradeService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    public interface IUpgradeService
    {
        event System.EventHandler<UpgradeVersionDescription> NewVersionAvailable;

        UpgradeVersionDescription UpgradeVersionDescription
        {
            get;
        }

        void CheckForUpgrade();

        void StartUpgrade();

        void CancelUpgrade();
    }
}
