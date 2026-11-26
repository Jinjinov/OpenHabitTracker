namespace OpenHabitTracker.Services;

// Hosts with no OS scheduler raise notifications from a timer while the app is open.
// Nothing that fell due before the app started is ever raised: the first tick's baseline is
// the moment the app started, which is the same rule as announcing nothing that was missed.
public abstract class RunningOnlyNotifications : INotifications, IAsyncDisposable
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    // The timer's ticks run on the thread pool, beside the calls from the page.
    private readonly object _requestsLock = new();

    private List<NotificationRequest> _requests = new();
    private DateTime _lastTick = DateTime.Now;

    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cts;

    public abstract bool CanNotify { get; }

    public bool CanScheduleWhileClosed => false;

    // The browser has no API for its own site settings, and the portal never refuses, so neither
    // host that runs on this base has a page to offer.
    public bool CanOpenSettings => false;

    private Action<string>? _onActivated;

    public abstract Task<bool> RequestPermission();

    public abstract Task<NotificationPermission> GetPermission();

    public Task OpenSettings()
    {
        return Task.CompletedTask;
    }

    public bool CanRequestExactTiming => false;

    public Task<bool> IsExactTimingAllowed()
    {
        return Task.FromResult(false);
    }

    public Task OpenExactTimingSettings()
    {
        return Task.CompletedTask;
    }

    protected abstract Task Show(NotificationRequest request);

    public void SetActivatedAction(Action<string> onActivated)
    {
        _onActivated = onActivated;
    }

    protected void OnActivated(string route)
    {
        _onActivated?.Invoke(route);
    }

    // A request that came due since the last tick is kept for the next one, because a new set never
    // holds a moment that has passed.
    public Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        DateTime now = DateTime.Now;

        lock (_requestsLock)
        {
            List<NotificationRequest> dueAndNotShown = _requests
                .Where(request => request.NotifyAt > _lastTick && request.NotifyAt <= now && requests.All(replacement => replacement.Id != request.Id))
                .ToList();

            _requests = [.. dueAndNotShown, .. requests];
        }

        if (requests.Count > 0)
            Start();

        return Task.CompletedTask;
    }

    public Task CancelAll()
    {
        lock (_requestsLock)
            _requests.Clear();

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();

        await OnDispose();

        GC.SuppressFinalize(this);
    }

    protected virtual Task OnDispose()
    {
        return Task.CompletedTask;
    }

    private void Start()
    {
        if (_timerTask is not null && !_timerTask.IsCompleted)
            return;

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(_interval);
        _timerTask = Tick();
    }

    private async Task Stop()
    {
        if (_timerTask is null || _cts is null)
            return;

        _cts.Cancel();
        await _timerTask;

        _cts.Dispose();
        _timer?.Dispose();

        _cts = null;
        _timer = null;
        _timerTask = null;
    }

    private async Task Tick()
    {
        try
        {
            if (_timer is null || _cts is null)
                return;

            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                DateTime now = DateTime.Now;

                List<NotificationRequest> due;

                lock (_requestsLock)
                {
                    due = _requests.Where(request => request.NotifyAt > _lastTick && request.NotifyAt <= now).ToList();

                    _requests.RemoveAll(due.Contains);

                    _lastTick = now;
                }

                // After the computer slept, the tick finds everything that came due meanwhile.
                foreach (NotificationRequest request in due.Where(request => now - request.NotifyAt <= request.AllowedDelay))
                    await Show(request);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
