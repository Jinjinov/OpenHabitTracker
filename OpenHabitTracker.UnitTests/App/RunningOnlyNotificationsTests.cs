using System.Reflection;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class RunningOnlyNotificationsTests
{
    private sealed class RecordingNotifications : RunningOnlyNotifications
    {
        private readonly List<NotificationRequest> _shown = [];

        public bool Disposed { get; private set; }

        public override bool CanNotify => true;

        public IReadOnlyList<NotificationRequest> Shown
        {
            get
            {
                lock (_shown)
                    return _shown.ToList();
            }
        }

        public override Task<bool> RequestPermission() => Task.FromResult(true);

        public override Task<NotificationPermission> GetPermission() => Task.FromResult(NotificationPermission.Allowed);

        protected override Task Show(NotificationRequest request)
        {
            lock (_shown)
                _shown.Add(request);

            return Task.CompletedTask;
        }

        public void Activate(string route) => OnActivated(route);

        protected override Task OnDispose()
        {
            Disposed = true;
            return Task.CompletedTask;
        }
    }

    private RecordingNotifications _sut = null!;

    // The production tick is 30 seconds, so the tests shorten it on the instance.
    [SetUp]
    public void SetUp()
    {
        _sut = new RecordingNotifications();

        typeof(RunningOnlyNotifications)
            .GetField("_interval", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, TimeSpan.FromMilliseconds(20));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.DisposeAsync();
    }

    private static NotificationRequest Request(string id, TimeSpan fromNow) =>
        new() { Id = id, Title = id, NotifyAt = DateTime.Now + fromNow, Route = $"/{id}" };

    private async Task WaitUntilShown(int count)
    {
        DateTime deadline = DateTime.Now.AddSeconds(10);

        while (_sut.Shown.Count < count && DateTime.Now < deadline)
            await Task.Delay(10);
    }

    [Test]
    public async Task Capabilities_OnlyNotifyingWhileRunning()
    {
        bool exactTimingAllowed = await _sut.IsExactTimingAllowed();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.CanScheduleWhileClosed, Is.False);
            Assert.That(_sut.CanOpenSettings, Is.False);
            Assert.That(_sut.CanRequestExactTiming, Is.False);
            Assert.That(exactTimingAllowed, Is.False);
        });
    }

    [Test]
    public async Task Replace_ShowsOnlyWhatFallsDueWhileRunningAndEachOnce()
    {
        NotificationRequest missed = Request("missed", TimeSpan.FromHours(-1));
        NotificationRequest due = Request("due", TimeSpan.FromMilliseconds(100));
        NotificationRequest later = Request("later", TimeSpan.FromHours(1));

        await _sut.Replace([missed, due, later]);

        await WaitUntilShown(1);
        await Task.Delay(200);

        Assert.That(_sut.Shown, Is.EqualTo(new[] { due }));
    }

    [Test]
    public async Task Replace_ASecondTime_ReplacesTheWholeSet()
    {
        NotificationRequest dropped = Request("dropped", TimeSpan.FromMilliseconds(150));
        NotificationRequest kept = Request("kept", TimeSpan.FromMilliseconds(150));

        await _sut.Replace([dropped]);
        await _sut.Replace([kept]);

        await WaitUntilShown(1);
        await Task.Delay(200);

        Assert.That(_sut.Shown, Is.EqualTo(new[] { kept }));
    }

    [Test]
    public async Task CancelAll_BeforeTheMoment_ShowsNothing()
    {
        await _sut.Replace([Request("cancelled", TimeSpan.FromMilliseconds(150))]);

        await _sut.CancelAll();
        await Task.Delay(400);

        Assert.That(_sut.Shown, Is.Empty);
    }

    [Test]
    public async Task DisposeAsync_StopsTheTimerAndRunsTheHostCleanup()
    {
        await _sut.Replace([Request("after dispose", TimeSpan.FromMilliseconds(200))]);

        await _sut.DisposeAsync();
        await Task.Delay(400);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Disposed, Is.True);
            Assert.That(_sut.Shown, Is.Empty);
        });
    }

    [Test]
    public void OnActivated_CallsTheLatestActionWithTheRoute()
    {
        List<string> first = [];
        List<string> second = [];

        _sut.Activate("/before any action");
        _sut.SetActivatedAction(first.Add);
        _sut.SetActivatedAction(second.Add);
        _sut.Activate("/tasks/3");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Empty);
            Assert.That(second, Is.EqualTo(new[] { "/tasks/3" }));
        });
    }

    [Test]
    public async Task NullHost_ReportsNoCapabilityAndIgnoresEveryCall()
    {
        Notifications sut = new();

        await sut.Replace([Request("x", TimeSpan.Zero)]);
        await sut.CancelAll();
        await sut.OpenSettings();
        await sut.OpenExactTimingSettings();
        sut.SetActivatedAction(_ => { });

        NotificationPermission permission = await sut.GetPermission();
        bool granted = await sut.RequestPermission();
        bool exactTimingAllowed = await sut.IsExactTimingAllowed();

        Assert.Multiple(() =>
        {
            Assert.That(sut.CanNotify, Is.False);
            Assert.That(sut.CanScheduleWhileClosed, Is.False);
            Assert.That(sut.CanOpenSettings, Is.False);
            Assert.That(sut.CanRequestExactTiming, Is.False);
            Assert.That(permission, Is.EqualTo(NotificationPermission.Unknown));
            Assert.That(granted, Is.False);
            Assert.That(exactTimingAllowed, Is.False);
        });
    }
}
