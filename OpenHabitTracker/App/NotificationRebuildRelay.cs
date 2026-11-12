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
}
