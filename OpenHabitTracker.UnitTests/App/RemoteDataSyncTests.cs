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
    public async Task StartLoginRetry_KeepsTryingUntilTheAttemptSaysStop()
    {
        int attempts = 0;
        TaskCompletionSource finished = new();

        _sut.StartLoginRetry(() =>
        {
            attempts++;

            if (attempts == 3)
                finished.SetResult();

            return Task.FromResult(attempts == 3);
        });

        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(100);

        Assert.That(attempts, Is.EqualTo(3));
    }

    [Test]
    public async Task StartLoginRetry_AttemptThrows_TriesAgain()
    {
        int attempts = 0;
        TaskCompletionSource finished = new();

        _sut.StartLoginRetry(() =>
        {
            attempts++;

            if (attempts == 1)
                throw new HttpRequestException("no route");

            finished.TrySetResult();

            return Task.FromResult(true);
        });

        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(attempts, Is.EqualTo(2));
    }

    [Test]
    public async Task StartLoginRetry_AlreadyRunning_DoesNotStartASecondLoop()
    {
        int first = 0, second = 0;
        TaskCompletionSource started = new();

        _sut.StartLoginRetry(() =>
        {
            first++;
            started.TrySetResult();
            return Task.FromResult(false);
        });

        _sut.StartLoginRetry(() =>
        {
            second++;
            return Task.FromResult(true);
        });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(100);

        Assert.That(second, Is.Zero);
    }

    [Test]
    public async Task Dispose_WhileRetrying_StopsTheLoop()
    {
        int attempts = 0;
        TaskCompletionSource started = new();

        _sut.StartLoginRetry(() =>
        {
            attempts++;
            started.TrySetResult();
            return Task.FromResult(false);
        });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await _sut.DisposeAsync();

        int stoppedAt = attempts;

        await Task.Delay(100);

        Assert.That(attempts, Is.EqualTo(stoppedAt));
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

    // Another device saves while this one is reloading after its previous save. The server stamps
    // LastChangeAt with its own clock, so a later poll must still see the second save.
    [Test]
    public async Task Poll_ChangeSavedDuringARefresh_IsPickedUpByALaterPoll()
    {
        List<NoteEntity> serverNotes = [];
        DateTime serverLastChangeAt = DateTime.UtcNow.AddMinutes(-1);
        int notesReads = 0;
        object gate = new();

        _remoteDataAccess.GetUsers().Returns(_ => Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 1, LastChangeAt = serverLastChangeAt }]));
        _remoteDataAccess.GetNotes().Returns(async _ =>
        {
            List<NoteEntity> snapshot;

            lock (gate)
            {
                notesReads++;
                snapshot = serverNotes.ToList();

                // the first refresh after the first save: the second save lands right after the notes were read
                if (notesReads == 1)
                {
                    serverNotes.Add(new NoteEntity { Id = 2, Title = "second", Content = "" });
                    serverLastChangeAt = DateTime.UtcNow;
                }
            }

            await Task.Delay(30); // the rest of the refresh

            return (IReadOnlyList<NoteEntity>)snapshot;
        });

        lock (gate)
        {
            serverNotes.Add(new NoteEntity { Id = 1, Title = "first", Content = "" });
            serverLastChangeAt = DateTime.UtcNow;
        }

        await Task.Delay(1000); // dozens of polls

        Assert.That(_clientState.Notes?.Values.Select(x => x.Title), Is.EquivalentTo(new[] { "first", "second" }));
    }

    [Test]
    public async Task Poll_NoChangeOnTheServer_DoesNotReloadOrRepaint()
    {
        int repaints = 0;
        _sut.SetRefreshAction(() => repaints++);
        _remoteDataAccess.ClearReceivedCalls();

        await Task.Delay(300); // a dozen polls

        Assert.That(repaints, Is.Zero);
        await _remoteDataAccess.Received().GetUsers();
        await _remoteDataAccess.DidNotReceive().GetNotes();
    }

    [Test]
    public async Task Poll_NewerChangeOnTheServer_ReloadsOnceAndRepaints()
    {
        TaskCompletionSource repainted = new();
        _sut.SetRefreshAction(() => repainted.TrySetResult());

        _remoteDataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>([new NoteEntity { Id = 4, Title = "from another device" }]));
        _remoteDataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 1, LastChangeAt = DateTime.UtcNow }]));

        await repainted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(200); // later polls see the same stamp

        Assert.That(_clientState.Notes!.Values.Select(x => x.Title), Is.EqualTo(new[] { "from another device" }));
        await _remoteDataAccess.Received(2).GetNotes(); // the switch to remote and this one change
    }

    [Test]
    public async Task Poll_ServerBackAfterAFailure_ClearsUnreachableAndRepaints()
    {
        int repaints = 0;
        TaskCompletionSource failed = new();
        TaskCompletionSource recovered = new();

        _sut.SetRefreshAction(() =>
        {
            repaints++;

            if (_sut.ServerUnreachable)
                failed.TrySetResult();
            else
                recovered.TrySetResult();
        });

        bool reachable = false;
        _remoteDataAccess.GetUsers().Returns<IReadOnlyList<UserEntity>>(_ => reachable ? [new UserEntity { Id = 1 }] : throw new HttpRequestException("no route"));

        await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // the failed tick slowed the poll to a minute, so the next success comes through a restart
        await _sut.StopPolling();
        reachable = true;

        Assert.That(_sut.ServerUnreachable, Is.False, "stopping resets the state");

        _sut.ReportServerUnreachable();
        _sut.StartPolling();

        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(_sut.ServerUnreachable, Is.False);
            Assert.That(repaints, Is.EqualTo(3));
        });
    }

    [Test]
    public void ReportServerUnreachable_Twice_RepaintsOnce()
    {
        int repaints = 0;
        _sut.SetRefreshAction(() => repaints++);

        _sut.ReportServerUnreachable();
        _sut.ReportServerUnreachable();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.ServerUnreachable, Is.True);
            Assert.That(repaints, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SetDataLocation_Local_StopsPolling()
    {
        await _sut.SetDataLocation(DataLocation.Local);
        _remoteDataAccess.ClearReceivedCalls();

        await Task.Delay(200);

        await _remoteDataAccess.DidNotReceive().GetUsers();
        Assert.That(_clientState.DataLocation, Is.EqualTo(DataLocation.Local));
    }

    // The switch reloads everything, so a server stamp from before it is no reason to reload again.
    [Test]
    public async Task SetDataLocation_Remote_TheStampReadByTheSwitchDoesNotTriggerAReload()
    {
        await _sut.SetDataLocation(DataLocation.Local);

        _remoteDataAccess.GetUsers().Returns(Task.FromResult<IReadOnlyList<UserEntity>>([new UserEntity { Id = 1, LastChangeAt = new DateTime(2026, 1, 1) }]));
        _remoteDataAccess.ClearReceivedCalls();

        await _sut.SetDataLocation(DataLocation.Remote);
        await Task.Delay(200);

        await _remoteDataAccess.Received(1).GetNotes();
    }
}
