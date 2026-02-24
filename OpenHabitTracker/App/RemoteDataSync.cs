using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.App;

public class RemoteDataSync(ClientState clientState) : IAsyncDisposable
{
    private readonly ClientState _clientState = clientState;

    private DateTime _lastRefreshAt;

    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cts;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    private Action? _refresh;

    public async ValueTask DisposeAsync()
    {
        await StopPolling();
    }

    public void SetRefreshAction(Action refresh)
    {
        _refresh = refresh;
    }

    public void StartPolling()
    {
        // Don't start if already running
        if (_timerTask is not null && !_timerTask.IsCompleted)
            return;

        // Create new instances for each start
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_interval);

        // Start the timer task
        _timerTask = ShortPolling();
    }

    public async Task StopPolling()
    {
        if (_timerTask is null || _cts is null)
            return;

        // Signal cancellation and wait for the task to complete
        _cts.Cancel();
        await _timerTask;

        // Dispose resources
        _cts.Dispose();
        _timer?.Dispose();

        // Clear references to allow garbage collection
        _cts = null;
        _timer = null;
        _timerTask = null;
    }

    private async Task ShortPolling()
    {
        try
        {
            if (_timer == null || _cts == null)
                return;

            // Continue running until cancellation is requested
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                IReadOnlyList<UserEntity> users = await _clientState.DataAccess.GetUsers();

                if (users.Count > 0)
                {
                    if (_lastRefreshAt < users[0].LastChangeAt)
                    {
                        await _clientState.RefreshState();
                        _lastRefreshAt = DateTime.UtcNow;

                        _refresh?.Invoke();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, clean up if necessary
        }
    }

    public async Task SetDataLocation(DataLocation dataLocation)
    {
        await _clientState.SetDataLocationAndRefresh(dataLocation);
        _lastRefreshAt = DateTime.UtcNow;

        if (_clientState.DataLocation == DataLocation.Remote)
        {
            StartPolling();
        }
        else
        {
            await StopPolling();
        }
    }
}
