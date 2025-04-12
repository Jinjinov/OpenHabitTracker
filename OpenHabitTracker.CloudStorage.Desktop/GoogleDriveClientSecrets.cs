using Google.Apis.Auth.OAuth2;

namespace OpenHabitTracker.CloudStorage.Desktop;

internal class GoogleDriveClientSecretsBase
{
    public virtual ClientSecrets ClientSecrets { get; } = new();
}

internal partial class GoogleDriveClientSecrets : GoogleDriveClientSecretsBase
{
}
