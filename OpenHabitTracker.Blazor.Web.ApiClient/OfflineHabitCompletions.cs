using Aiursoft.AiurEventSyncer.Abstract;
using Aiursoft.AiurEventSyncer.Models;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public sealed class OfflineHabitCompletions(IOfflineHabitStore store, DataAccessClient client, ApiClientOptions options, [FromKeyedServices(DataAccessKeys.Local)] IDataAccess localDataAccess) : IOfflineHabitCompletions
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public async Task RefreshAndSync(ClientState state)
    {
        if (state.DataLocation != DataLocation.Remote || state.Habits is null || string.IsNullOrEmpty(options.Username))
            return;

        var settings = (await localDataAccess.GetSettings()).FirstOrDefault();
        if (settings is null || !settings.RememberMe || string.IsNullOrEmpty(settings.RefreshToken) ||
            !SameServer(settings.BaseUrl, options.BaseUrl) ||
            !string.Equals(settings.Username, options.Username, StringComparison.OrdinalIgnoreCase))
            return;

        OfflineHabitSnapshot snapshot = new()
        {
            Server = options.BaseUrl,
            Username = options.Username,
            UserId = state.User.Id,
            SavedAt = DateTimeOffset.UtcNow,
            Habits = state.Habits.Where(x => !x.Value.IsDeleted).Select(x => new SavedHabit
            {
                Id = x.Key,
                Title = x.Value.Title,
                TargetQuantity = x.Value.TargetQuantity,
                Duration = x.Value.Duration
            }).ToList()
        };
        await store.WriteSnapshot(snapshot);
        await Synchronize(snapshot);
    }

    public Task<OfflineHabitSnapshot?> ReadSnapshot(string server, string username) => store.ReadSnapshot(server, username);

    public Task<IReadOnlyList<HabitCompletion>> ReadPending(OfflineHabitSnapshot snapshot) => store.ReadPending(snapshot);

    public Task Record(OfflineHabitSnapshot snapshot, SavedHabit habit)
    {
        if (!snapshot.Habits.Any(x => x.Id == habit.Id) || habit.TargetQuantity <= 0)
            throw new InvalidOperationException("This habit is not available for offline check-ins.");
        DateTime now = DateTime.Now;
        DateTime startedAt = now - (habit.Duration?.ToTimeSpan() ?? TimeSpan.Zero);
        return store.AddPending(snapshot, new()
        {
            OperationId = Guid.NewGuid(),
            UserId = snapshot.UserId,
            HabitId = habit.Id,
            StartedAt = startedAt < now.Date ? now.Date : startedAt,
            CompletedAt = now,
            Quantity = habit.TargetQuantity
        });
    }

    public async Task Synchronize(OfflineHabitSnapshot snapshot)
    {
        if (string.IsNullOrEmpty(options.BearerToken) ||
            !SameServer(snapshot.Server, options.BaseUrl) ||
            !string.Equals(snapshot.Username, options.Username, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Connect to the saved account before synchronizing its check-ins.");

        await _syncLock.WaitAsync();
        try
        {
            // Restore the durable, immutable operation log. A new remote starts at its first
            // unacknowledged commit; acknowledgements are removed only after the server replies.
            Repository<HabitCompletion> repository = new();
            foreach (HabitCompletion completion in await store.ReadPending(snapshot))
            {
                repository.CommitObject(new Commit<HabitCompletion>
                {
                    Id = completion.OperationId.ToString("D"),
                    Item = completion
                });
            }
            var connection = new CompletionConnection(async completions =>
            {
                foreach (Commit<HabitCompletion> commit in completions)
                {
                    if (commit.Item.UserId != snapshot.UserId)
                        throw new InvalidOperationException("A pending check-in belongs to another account.");
                    await client.RecordHabitCompletion(commit.Item);
                    await store.RemovePending(snapshot, commit.Item.OperationId);
                }
                return true;
            });
            CompletionRemote remote = new(connection);
            await remote.AttachAsync(repository);
            await remote.PushAsync();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private static bool SameServer(string first, string second) =>
        string.Equals(new Uri(first).AbsoluteUri.TrimEnd('/'), new Uri(second).AbsoluteUri.TrimEnd('/'), StringComparison.Ordinal);

    private sealed class CompletionRemote(IConnectionProvider<HabitCompletion> connection) : Remote<HabitCompletion>(connection);

    private sealed class CompletionConnection(Func<List<Commit<HabitCompletion>>, Task<bool>> upload) : IConnectionProvider<HabitCompletion>
    {
        public event Action? OnReconnecting { add { } remove { } }
        public Task<bool> Upload(List<Commit<HabitCompletion>> commits) => upload(commits);
        public Task<List<Commit<HabitCompletion>>> Download(string pointer) => throw new NotSupportedException();
        public Task PullAndMonitor(Func<List<Commit<HabitCompletion>>, Task> onData, Func<string> startPositionFactory,
            Func<Task> onConnected, bool monitorInCurrentThread) => throw new NotSupportedException();
        public Task Disconnect() => Task.CompletedTask;
    }
}

public partial class DataAccessClient
{
    // Uses the existing token-refresh handler and base address, just like the generated methods.
    internal async Task<long> RecordHabitCompletion(HabitCompletion completion)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, (Uri?)null)
        {
            Content = JsonContent.Create(completion)
        };
        StringBuilder url = new("api/habit-completions");
        PrepareRequest(_httpClient, request, url);
        request.RequestUri = new Uri(url.ToString(), UriKind.RelativeOrAbsolute);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            throw new SignedOutException();
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<long>();
    }
}
