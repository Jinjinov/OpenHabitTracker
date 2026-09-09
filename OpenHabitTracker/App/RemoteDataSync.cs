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
    private readonly TimeSpan _unreachableInterval = TimeSpan.FromSeconds(60);

    private Action? _refresh;

    // True once a tick has failed, false again after one succeeds. The nav icon renders from it.
    public bool ServerUnreachable { get; private set; }

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

        ServerUnreachable = false;

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
                try
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

                    _timer.Period = _interval;

                    SetServerUnreachable(false);
                }
                // The tick failed, most likely an unreachable server. Skip it, slow down, let the
                // next tick retry. The filter keeps a real cancellation falling through to the
                // outer clause, since a request timeout is also an OperationCanceledException.
                catch (Exception) when (!_cts.Token.IsCancellationRequested)
                {
                    _timer.Period = _unreachableInterval;

                    SetServerUnreachable(true);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, clean up if necessary
        }
    }

    // Only a change repaints, or the UI re-renders on every tick.
    private void SetServerUnreachable(bool serverUnreachable)
    {
        if (ServerUnreachable == serverUnreachable)
            return;

        ServerUnreachable = serverUnreachable;

        _refresh?.Invoke();
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
