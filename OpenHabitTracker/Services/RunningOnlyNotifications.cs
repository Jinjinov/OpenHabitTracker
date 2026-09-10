namespace OpenHabitTracker.Services;

// Hosts with no OS scheduler raise notifications from a timer while the app is open.
// Nothing that fell due before the app started is ever raised: the first tick's baseline is
// the moment the app started, which is the same rule as announcing nothing that was missed.
public abstract class RunningOnlyNotifications : INotifications, IAsyncDisposable
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    private List<NotificationRequest> _requests = new();
    private DateTime _lastTick = DateTime.Now;

    private PeriodicTimer? _timer;
    private Task? _timerTask;
    private CancellationTokenSource? _cts;

    public abstract bool CanNotify { get; }

    public bool CanScheduleWhileClosed => false;

    private Action<string>? _onActivated;

    public abstract Task<bool> RequestPermission();

    protected abstract Task Show(NotificationRequest request);

    public void SetActivatedAction(Action<string> onActivated)
    {
        _onActivated = onActivated;
    }

    protected void OnActivated(string route)
    {
        _onActivated?.Invoke(route);
    }

    public Task Replace(IReadOnlyList<NotificationRequest> requests)
    {
        _requests = requests.ToList();

        if (_requests.Count > 0)
            Start();

        return Task.CompletedTask;
    }

    public Task CancelAll()
    {
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

                List<NotificationRequest> due = _requests.Where(request => request.NotifyAt > _lastTick && request.NotifyAt <= now).ToList();

                _lastTick = now;

                foreach (NotificationRequest request in due)
                {
                    _requests.Remove(request);

                    await Show(request);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
