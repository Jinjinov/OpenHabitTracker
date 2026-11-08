using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OpenHabitTracker.UnitTests.ApiClient;

[TestFixture]
public class OfflineHabitCompletionTests
{
    private sealed class JsonStore : IOfflineHabitStore
    {
        private readonly Dictionary<Guid, string> _pending = new();
        public bool FailWrite { get; set; }
        public Task<OfflineHabitSnapshot?> ReadSnapshot(string server, string username) => Task.FromResult<OfflineHabitSnapshot?>(null);
        public Task WriteSnapshot(OfflineHabitSnapshot snapshot) => Task.CompletedTask;
        public Task<IReadOnlyList<HabitCompletion>> ReadPending(OfflineHabitSnapshot snapshot) =>
            Task.FromResult<IReadOnlyList<HabitCompletion>>(_pending.Values.Select(x => JsonSerializer.Deserialize<HabitCompletion>(x)!)
                .Where(x => x.UserId == snapshot.UserId).ToList());
        public Task AddPending(OfflineHabitSnapshot snapshot, HabitCompletion completion)
        {
            if (FailWrite) throw new IOException("Storage unavailable");
            _pending.Add(completion.OperationId, JsonSerializer.Serialize(completion));
            return Task.CompletedTask;
        }
        public Task RemovePending(OfflineHabitSnapshot snapshot, Guid id)
        {
            _pending.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }

    private static OfflineHabitSnapshot Snapshot() => new()
    {
        Server = "https://habits.example/", Username = "alice", UserId = 1,
        Habits = [new() { Id = 11, Title = "Read", TargetQuantity = 2 }]
    };

    private static ApiClientOptions Session() => new() { BaseUrl = "https://habits.example/", Username = "alice", BearerToken = "test-token" };

    private static OfflineHabitCompletions Service(IOfflineHabitStore store, HttpClient http, ApiClientOptions? options = null)
    {
        options ??= Session();
        return new(store, new DataAccessClient(http, options), options, Substitute.For<IDataAccess>());
    }

    [Test]
    public async Task OfflineCompletion_IsStillPendingAfterCreatingANewSession()
    {
        JsonStore store = new();
        using HttpClient http = new(new Handler(_ => throw new HttpRequestException()));
        OfflineHabitSnapshot snapshot = Snapshot();
        await Service(store, http).Record(snapshot, snapshot.Habits[0]);
        IReadOnlyList<HabitCompletion> restored = await Service(store, http).ReadPending(snapshot);
        Assert.Multiple(() =>
        {
            Assert.That(restored, Has.Count.EqualTo(1));
            Assert.That(restored[0].OperationId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(restored[0].UserId, Is.EqualTo(1));
            Assert.That(restored[0].HabitId, Is.EqualTo(11));
            Assert.That(restored[0].Quantity, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task LostResponse_RetainsTheSameOperationIdForRetry()
    {
        JsonStore store = new();
        HashSet<Guid> saved = [];
        List<Guid> attempts = [];
        bool loseResponse = true;
        using HttpClient http = new(new Handler(async request =>
        {
            Assert.That(request.RequestUri!.AbsoluteUri, Is.EqualTo("https://habits.example/api/habit-completions"));
            Assert.That(request.Headers.Authorization!.Parameter, Is.EqualTo("test-token"));
            HabitCompletion completion = (await request.Content!.ReadFromJsonAsync<HabitCompletion>())!;
            saved.Add(completion.OperationId);
            attempts.Add(completion.OperationId);
            if (loseResponse) { loseResponse = false; throw new HttpRequestException("Response lost after saving"); }
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(21L) };
        }));
        OfflineHabitSnapshot snapshot = Snapshot();
        await Service(store, http).Record(snapshot, snapshot.Habits[0]);
        Assert.ThrowsAsync<HttpRequestException>(() => Service(store, http).Synchronize(snapshot));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
        await Service(store, http).Synchronize(snapshot);
        Assert.Multiple(() =>
        {
            Assert.That(saved, Has.Count.EqualTo(1));
            Assert.That(attempts, Has.Count.EqualTo(2));
            Assert.That(attempts[0], Is.EqualTo(attempts[1]));
        });
        Assert.That(await store.ReadPending(snapshot), Is.Empty);
    }

    [Test]
    public async Task PartialUpload_OnlyRemovesAcknowledgedCompletions()
    {
        JsonStore store = new();
        int attempts = 0;
        using HttpClient http = new(new Handler(_ =>
        {
            if (++attempts == 2) throw new HttpRequestException();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(21L) });
        }));
        OfflineHabitSnapshot snapshot = Snapshot();
        await Service(store, http).Record(snapshot, snapshot.Habits[0]);
        await Service(store, http).Record(snapshot, snapshot.Habits[0]);
        Assert.ThrowsAsync<HttpRequestException>(() => Service(store, http).Synchronize(snapshot));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
        await Service(store, http).Synchronize(snapshot);
        Assert.That(await store.ReadPending(snapshot), Is.Empty);
        Assert.That(attempts, Is.EqualTo(3));
    }

    [Test]
    public async Task SwitchingAccountOrServer_DoesNotSendOldPendingRecords()
    {
        JsonStore store = new();
        using HttpClient http = new(new Handler(_ => throw new AssertionException("Request must not be sent")));
        OfflineHabitSnapshot snapshot = Snapshot();
        await Service(store, http).Record(snapshot, snapshot.Habits[0]);
        ApiClientOptions other = Session();
        other.Username = "bob";
        Assert.ThrowsAsync<InvalidOperationException>(() => Service(store, http, other).Synchronize(snapshot));
        other.Username = "alice";
        other.BaseUrl = "https://another.example/";
        Assert.ThrowsAsync<InvalidOperationException>(() => Service(store, http, other).Synchronize(snapshot));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RejectedSession_DoesNotDiscardPendingRecords()
    {
        JsonStore store = new();
        using HttpClient http = new(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));
        OfflineHabitSnapshot snapshot = Snapshot();
        await Service(store, http).Record(snapshot, snapshot.Habits[0]);
        Assert.ThrowsAsync<SignedOutException>(() => Service(store, http).Synchronize(snapshot));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AccountChangesDuringUpload_LeavesRemainingRecordsForTheirOriginalAccount()
    {
        JsonStore store = new();
        ApiClientOptions options = Session();
        int attempts = 0;
        using HttpClient http = new(new Handler(_ =>
        {
            attempts++;
            options.BaseUrl = "https://another.example/";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(21L) });
        }));
        var service = Service(store, http, options);
        var snapshot = Snapshot();
        await service.Record(snapshot, snapshot.Habits[0]);
        await service.Record(snapshot, snapshot.Habits[0]);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.Synchronize(snapshot));
        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueuedSync_RechecksTheAccountAfterWaitingForAnUpload()
    {
        JsonStore store = new();
        ApiClientOptions options = Session();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        using HttpClient http = new(new Handler(async _ =>
        {
            attempts++;
            entered.TrySetResult();
            await release.Task;
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(21L) };
        }));
        var service = Service(store, http, options);
        var snapshot = Snapshot();
        await service.Record(snapshot, snapshot.Habits[0]);
        Task first = service.Synchronize(snapshot);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.Record(snapshot, snapshot.Habits[0]);
        Task queued = service.Synchronize(snapshot);
        options.Username = "bob";
        release.SetResult();
        await first;
        Assert.ThrowsAsync<InvalidOperationException>(() => queued);
        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task AccountChangesDuringTokenRefresh_DoesNotSendOrRetryWithTheOtherSession(bool afterUnauthorized)
    {
        JsonStore store = new();
        ApiClientOptions options = Session();
        options.BearerTokenExpiresAt = afterUnauthorized ? DateTimeOffset.UtcNow.AddDays(1) : DateTimeOffset.UtcNow.AddMinutes(1);
        int attempts = 0;
        var refresher = Substitute.For<ITokenRefresher>();
        refresher.TryRefresh().Returns(_ =>
        {
            options.Username = "bob";
            options.BearerToken = "other-account-token";
            return Task.FromResult(true);
        });
        using HttpClient http = new(new RefreshTokenHandler(options, refresher)
        {
            InnerHandler = new Handler(_ =>
            {
                attempts++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            })
        });
        var service = Service(store, http, options);
        var snapshot = Snapshot();
        await service.Record(snapshot, snapshot.Habits[0]);
        Assert.ThrowsAsync<InvalidOperationException>(() => service.Synchronize(snapshot));
        Assert.That(attempts, Is.EqualTo(afterUnauthorized ? 1 : 0));
        Assert.That(await store.ReadPending(snapshot), Has.Count.EqualTo(1));
    }

    [Test]
    public void FailedDurableWrite_IsReportedInsteadOfPretendingTheCheckInWasSaved()
    {
        JsonStore store = new() { FailWrite = true };
        using HttpClient http = new(new Handler(_ => throw new AssertionException("Request must not be sent")));
        OfflineHabitSnapshot snapshot = Snapshot();
        Assert.ThrowsAsync<IOException>(() => Service(store, http).Record(snapshot, snapshot.Habits[0]));
    }
}
