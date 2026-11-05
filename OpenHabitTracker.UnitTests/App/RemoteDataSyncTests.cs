using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class RemoteDataSyncTests
{
    private IDataAccess _remoteDataAccess = null!;
    private ClientState _clientState = null!;
    private RemoteDataSync _sut = null!;

    private static IDataAccess CreateDataAccess(DataLocation dataLocation)
    {
        IDataAccess dataAccess = Substitute.For<IDataAccess>();

        dataAccess.DataLocation.Returns(dataLocation);
        dataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 1 }]));
        dataAccess.GetSettings().Returns(Task.FromResult<IReadOnlyList<SettingsEntity>>([new SettingsEntity { Id = 1 }]));
        dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
        dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>([]));
        dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([]));
        dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>([]));
        dataAccess.GetCategories().Returns(Task.FromResult<IReadOnlyList<CategoryEntity>>([]));

        return dataAccess;
    }

    [SetUp]
    public async Task SetUp()
    {
        _remoteDataAccess = CreateDataAccess(DataLocation.Remote);

        _clientState = new([CreateDataAccess(DataLocation.Local), _remoteDataAccess], new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));

        _sut = new(_clientState) { PollInterval = TimeSpan.FromMilliseconds(20) };

        await _sut.SetDataLocation(DataLocation.Remote);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.DisposeAsync();
    }

    [Test]
    public async Task Poll_SignedOut_RunsTheActionAndLogoutDoesNotDeadlockOnThePoller()
    {
        TaskCompletionSource loggedOut = new();

        _sut.SetSignedOutAction(async () =>
        {
            await _sut.SetDataLocation(DataLocation.Local);
            loggedOut.SetResult();
        });

        _remoteDataAccess.GetUsers().Returns<IReadOnlyList<UserEntity>>(_ => throw new SignedOutException());

        await loggedOut.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Local));
            Assert.That(_sut.ServerUnreachable, Is.False);
        });
    }

    [Test]
    public async Task Poll_ConnectionFails_ReportsUnreachableAndNotSignedOut()
    {
        bool signedOut = false;

        _sut.SetSignedOutAction(() =>
        {
            signedOut = true;
            return Task.CompletedTask;
        });

        TaskCompletionSource failed = new();

        _sut.SetRefreshAction(() => failed.TrySetResult());

        _remoteDataAccess.GetUsers().Returns<IReadOnlyList<UserEntity>>(_ => throw new HttpRequestException("no route"));

        await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(_sut.ServerUnreachable, Is.True);
            Assert.That(signedOut, Is.False);
            Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Remote));
        });
    }

    [Test]
    public async Task StopPolling_RequestFailsAfterTheStop_DoesNotThrow()
    {
        TaskCompletionSource requestStarted = new();
        TaskCompletionSource<IReadOnlyList<UserEntity>> request = new();

        _remoteDataAccess.GetUsers().Returns(_ =>
        {
            requestStarted.TrySetResult();
            return request.Task;
        });

        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Task stopping = _sut.StopPolling();

        request.SetException(new HttpRequestException("connection refused"));

        await stopping.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task ReportSignedOut_TwoCallersAtOnce_RunsTheActionOnce()
    {
        int calls = 0;
        TaskCompletionSource release = new();

        _sut.SetSignedOutAction(async () =>
        {
            calls++;
            await release.Task;
        });

        Task first = _sut.ReportSignedOut();
        Task second = _sut.ReportSignedOut();

        release.SetResult();

        await Task.WhenAll(first, second);

        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public async Task ReportSignedOut_AlreadyLocal_DoesNothing()
    {
        await _sut.SetDataLocation(DataLocation.Local);

        bool signedOut = false;

        _sut.SetSignedOutAction(() =>
        {
            signedOut = true;
            return Task.CompletedTask;
        });

        await _sut.ReportSignedOut();

        Assert.That(signedOut, Is.False);
    }
}
