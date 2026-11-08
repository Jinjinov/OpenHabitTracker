using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.App;

public class RemoteDataSync(ClientState clientState, IOfflineHabitCompletions? offlineHabits = null) : IAsyncDisposable
{
    private readonly ClientState _clientState = clientState;

    private DateTime _lastRefreshAt;

    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cts;
    private readonly TimeSpan _unreachableInterval = TimeSpan.FromSeconds(60);

    private Action? _refresh;
    private Func<Task>? _signedOut;
    private bool _signingOut;

    private Task? _loginRetryTask;
    private CancellationTokenSource? _loginRetryCts;

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(10);

    // True once a tick has failed, false again after one succeeds. The nav icon renders from it.
    public bool ServerUnreachable { get; private set; }
    public bool OfflineSyncFailed { get; private set; }

    private async Task SyncOfflineHabits()
    {
        if (offlineHabits is null)
            return;
        bool previouslyFailed = OfflineSyncFailed;
        try
        {
            await offlineHabits.RefreshAndSync(_clientState);
            OfflineSyncFailed = false;
        }
        catch (SignedOutException)
        {
            throw;
        }
        catch (Exception)
        {
            // A failed cache write or upload must not erase the durable queue or break online use.
            OfflineSyncFailed = true;
        }
        finally
        {
            if (previouslyFailed != OfflineSyncFailed)
                _refresh?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopPolling();

        if (_loginRetryTask is null || _loginRetryCts is null)
            return;

        _loginRetryCts.Cancel();
        await _loginRetryTask;

        _loginRetryCts.Dispose();

        _loginRetryCts = null;
        _loginRetryTask = null;
    }

    // For an app that started with no connection. The attempt returns true when there is nothing left to retry.
    public void StartLoginRetry(Func<Task<bool>> attempt)
    {
        if (_loginRetryTask is not null && !_loginRetryTask.IsCompleted)
            return;

        _loginRetryCts = new CancellationTokenSource();
        _loginRetryTask = RetryLogin(attempt, _loginRetryCts.Token);
    }

    private async Task RetryLogin(Func<Task<bool>> attempt, CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(PollInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    if (await attempt())
                        return;
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // Still no working connection, so the next tick tries again.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, clean up if necessary
        }
    }

    public void SetRefreshAction(Action refresh)
    {
        _refresh = refresh;
    }

    public void SetSignedOutAction(Func<Task> signedOut)
    {
        _signedOut = signedOut;
    }

    public void StartPolling()
    {
        // Don't start if already running
        if (_timerTask is not null && !_timerTask.IsCompleted)
            return;

        // Create new instances for each start
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(PollInterval);

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
        bool signedOut = false;

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
                        // Skipped while a refresh started elsewhere is running - a delete-all stamps
                        // LastChangeAt and then refreshes itself - because two refreshes overlapping
                        // null each other's dictionaries. The next tick still sees the change.
                        if (!_clientState.IsRefreshing && _lastRefreshAt < users[0].LastChangeAt)
                        {
                            await _clientState.RefreshState();
                            _lastRefreshAt = DateTime.UtcNow;

                            _refresh?.Invoke();
                        }
                    }

                    await SyncOfflineHabits();

                    _timer.Period = PollInterval;

                    SetServerUnreachable(false);
                }
                catch (SignedOutException)
                {
                    signedOut = true;
                    break;
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
        catch (Exception) when (_cts?.IsCancellationRequested == true)
        {
            // A request that was in flight when polling stopped, which nobody is waiting for any more.
        }

        // Not awaited: the action stops polling, which waits for this method to finish.
        if (signedOut)
            _ = ReportSignedOut();
    }

    // A call that failed outside the poller knows before the next tick does.
    public void ReportServerUnreachable() => SetServerUnreachable(true);

    // The poller and a user action can both find out at the same time.
    public async Task ReportSignedOut()
    {
        if (_signingOut || _signedOut is null || _clientState.DataLocation != DataLocation.Remote)
            return;

        _signingOut = true;

        try
        {
            await _signedOut();
        }
        finally
        {
            _signingOut = false;
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
        // Stopped before the switch, so a tick's refresh in flight finishes on the remote data
        // before the local refresh starts - two refreshes overlapping null each other's dictionaries.
        if (dataLocation == DataLocation.Local)
        {
            await StopPolling();
        }

        await _clientState.SetDataLocationAndRefresh(dataLocation);
        _lastRefreshAt = DateTime.UtcNow;

        if (_clientState.DataLocation == DataLocation.Remote)
        {
            await SyncOfflineHabits();
            StartPolling();
        }
    }
}
