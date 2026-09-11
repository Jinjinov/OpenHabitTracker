using Microsoft.JSInterop;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.Blazor.Wasm;

// The browser can only raise a notification while a page is open, and there is no local
// scheduling primitive on the web: Notification Triggers was abandoned and Web Push needs a server.
public sealed class Notifications(IJsInterop jsInterop) : RunningOnlyNotifications
{
    private readonly IJsInterop _jsInterop = jsInterop;

    private DotNetObjectReference<Notifications>? _selfReference;

    public override bool CanNotify => true;

    public override async Task<bool> RequestPermission()
    {
        return await _jsInterop.RequestNotificationPermission();
    }

    public override async Task<NotificationPermission> GetPermission()
    {
        return await _jsInterop.GetNotificationPermission() switch
        {
            "granted" => NotificationPermission.Allowed,
            "denied" => NotificationPermission.Blocked,
            "default" => NotificationPermission.NotAsked,
            _ => NotificationPermission.Unknown
        };
    }

    protected override async Task Show(NotificationRequest request)
    {
        _selfReference ??= DotNetObjectReference.Create(this);

        await _jsInterop.ShowNotification(request.Title, request.Body, request.Route, _selfReference);
    }

    [JSInvokable]
    public void OnNotificationActivated(string route)
    {
        OnActivated(route);
    }

    protected override Task OnDispose()
    {
        _selfReference?.Dispose();
        _selfReference = null;

        return Task.CompletedTask;
    }
}
