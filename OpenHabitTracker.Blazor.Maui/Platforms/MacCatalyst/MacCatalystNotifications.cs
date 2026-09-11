using System.Threading.Tasks;

namespace OpenHabitTracker.Blazor.Maui;

// On Mac Catalyst the iOS settings URL opens the auto-generated settings-bundle screen rather
// than System Settings, so the notifications pane is opened by its own URL scheme instead. The
// user still picks the app from the list there.
public sealed class MacCatalystNotifications : PluginNotifications
{
    private const string NotificationsPane = "x-apple.systempreferences:com.apple.preference.notifications";

    public override async Task OpenSettings()
    {
        await Launcher.Default.OpenAsync(NotificationsPane);
    }
}
