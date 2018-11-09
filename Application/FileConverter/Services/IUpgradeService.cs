
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
    }
}
