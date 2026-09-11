using OpenHabitTracker.Services;
using System.Threading.Tasks;
using UserNotifications;

namespace OpenHabitTracker.Blazor.Maui;

// Only to report the three states the row shows: the plugin reports a boolean, so a permission
// that has never been asked for would read the same as one that was refused. macCatalyst carries
// the identical override, because sharing one file across two platform folders would need a
// per-platform condition in the csproj.
public sealed class IosNotifications : PluginNotifications
{
    public override async Task<NotificationPermission> GetPermission()
    {
        UNNotificationSettings settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();

        return settings.AuthorizationStatus switch
        {
            UNAuthorizationStatus.NotDetermined => NotificationPermission.NotAsked,
            UNAuthorizationStatus.Denied => NotificationPermission.Blocked,
            // Authorized, Provisional and Ephemeral all deliver something.
            _ => NotificationPermission.Allowed
        };
    }
}
