using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenHabitTracker.App;

// Takes a schedule rebuild from outside the UI, like a resumed window, into the scope the UI runs in,
// where the settings and the data are the ones on screen, from the sync server when signed in.
public class NotificationRebuildRelay
{
    private Func<Task>? _rebuild;

    public void SetRebuildAction(Func<Task> rebuild)
    {
        _rebuild = rebuild;
    }

    // False when no UI has set the action, like for a broadcast that started the app without a window.
    public async Task<bool> TryRebuild()
    {
        if (_rebuild is null)
            return false;

        await _rebuild();

        return true;
    }

    // Rebuilds from the root services of a host, which has the relay as a singleton.
    public static async Task Rebuild(IServiceProvider services)
    {
        try
        {
            // A new scope reads the device's own database, which is not the data of a signed in user.
            if (await services.GetRequiredService<NotificationRebuildRelay>().TryRebuild())
                return;

            // Without a UI, its own scope keeps the loaded data from outliving the rebuild.
            using IServiceScope scope = services.CreateScope();

            await scope.ServiceProvider.GetRequiredService<INotificationScheduler>().Rebuild();
        }
        catch (Exception exception)
        {
            // A failed rebuild must never take the app down on resume.
            services.GetRequiredService<ILogger<NotificationRebuildRelay>>().LogError(exception, "Notification schedule rebuild failed");
        }
    }
}
