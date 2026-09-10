using OpenHabitTracker.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace OpenHabitTracker.Blazor.Photino;

// Linux has no OS scheduler for notifications: the portal exposes AddNotification and
// RemoveNotification and nothing else, so this host fires from a timer while it runs.
// The portal is used rather than org.freedesktop.Notifications directly, because Flatpak's
// default bus policy allows org.freedesktop.portal.* and the manifest carries no --talk-name.
public sealed class Notifications : RunningOnlyNotifications
{
    private const string PortalService = "org.freedesktop.portal.Desktop";
    private const string PortalPath = "/org/freedesktop/portal/desktop";
    private const string PortalInterface = "org.freedesktop.portal.Notification";

    private bool _connected;

    private bool _watching;

    // The portal reports an invoked action by notification id, so the route is kept beside it.
    private readonly Dictionary<string, string> _routesById = new();

    public override bool CanNotify => OperatingSystem.IsLinux();

    // The portal has no permission prompt of its own; the desktop decides whether to show it.
    public override Task<bool> RequestPermission()
    {
        return Task.FromResult(CanNotify);
    }

    protected override async Task Show(NotificationRequest request)
    {
        try
        {
            DBusConnection connection = DBusConnection.Session;

            if (!_connected)
            {
                await connection.ConnectAsync();
                _connected = true;
            }

            await WatchActions(connection);

            _routesById[request.Id] = request.Route;

            MessageWriter writer = connection.GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: PortalService,
                path: PortalPath,
                @interface: PortalInterface,
                member: "AddNotification",
                signature: "sa{sv}",
                flags: MessageFlags.None);

            writer.WriteString(request.Id);

            ArrayStart dictionary = writer.WriteDictionaryStart();

            writer.WriteDictionaryEntryStart();
            writer.WriteString("title");
            writer.WriteVariantString(request.Title);

            if (!string.IsNullOrEmpty(request.Body))
            {
                writer.WriteDictionaryEntryStart();
                writer.WriteString("body");
                writer.WriteVariantString(request.Body);
            }

            // Without a default action the portal has nothing to report when the body is clicked.
            writer.WriteDictionaryEntryStart();
            writer.WriteString("default-action");
            writer.WriteVariantString("open");

            writer.WriteDictionaryEnd(dictionary);

            MessageBuffer message = writer.CreateMessage();

            await connection.CallMethodAsync(message);
        }
        catch (Exception)
        {
            // A desktop with no notification portal is not a reason to break the app.
        }
    }

    private async Task WatchActions(DBusConnection connection)
    {
        if (_watching)
            return;

        _watching = true;

        await connection.WatchSignalAsync(
            sender: PortalService,
            path: PortalPath,
            @interface: PortalInterface,
            signal: "ActionInvoked",
            reader: ReadNotificationId,
            handler: OnActionInvoked,
            readerState: null,
            emitOnCapturedContext: false,
            flags: ObserverFlags.None);
    }

    private static string ReadNotificationId(Message message, object? state)
    {
        Reader reader = message.GetBodyReader();

        return reader.ReadString();
    }

    private void OnActionInvoked(Exception? error, string notificationId)
    {
        if (error is null && _routesById.TryGetValue(notificationId, out string? route))
            OnActivated(route);
    }
}
