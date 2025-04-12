using Google.Apis.Drive.v3;
using System.Threading.Tasks;

namespace OpenHabitTracker.CloudStorage.Desktop;

public interface IGoogleDriveService
{
    Task<DriveService?> GetDriveService();
}
