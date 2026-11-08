using Microsoft.JSInterop;
using OpenHabitTracker.App;
using System.Text;
using System.Text.Json;

namespace OpenHabitTracker.Blazor;

public sealed class OfflineHabitStore(IJSRuntime js) : IOfflineHabitStore
{
    private static string Key(string server, string username) => "OpenHabitTracker:offline-habits:" +
        Convert.ToBase64String(Encoding.UTF8.GetBytes(new Uri(server).AbsoluteUri.TrimEnd('/') + "|" + username.Trim().ToUpperInvariant()));

    private static string PendingPrefix(OfflineHabitSnapshot snapshot) => Key(snapshot.Server, snapshot.Username) + ":" + snapshot.UserId + ":";

    public async Task<OfflineHabitSnapshot?> ReadSnapshot(string server, string username)
    {
        string? json = await js.InvokeAsync<string?>("localStorage.getItem", Key(server, username) + ":snapshot");
        return json is null ? null : JsonSerializer.Deserialize<OfflineHabitSnapshot>(json);
    }

    public async Task WriteSnapshot(OfflineHabitSnapshot snapshot) =>
        await js.InvokeVoidAsync("localStorage.setItem", Key(snapshot.Server, snapshot.Username) + ":snapshot", JsonSerializer.Serialize(snapshot));

    public async Task<IReadOnlyList<HabitCompletion>> ReadPending(OfflineHabitSnapshot snapshot)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/OpenHabitTracker.Blazor/jsInterop.js");
        string[] entries = await module.InvokeAsync<string[]>("readOfflineHabitCompletions", PendingPrefix(snapshot));
        return entries.Select(json => JsonSerializer.Deserialize<HabitCompletion>(json) ?? throw new JsonException())
            .OrderBy(x => x.CompletedAt).ThenBy(x => x.OperationId).ToList();
    }

    public async Task AddPending(OfflineHabitSnapshot snapshot, HabitCompletion completion)
    {
        if (completion.UserId != snapshot.UserId)
            throw new InvalidOperationException("The completion belongs to another account.");
        await js.InvokeVoidAsync("localStorage.setItem", PendingPrefix(snapshot) + completion.OperationId.ToString("D"), JsonSerializer.Serialize(completion));
    }

    public async Task RemovePending(OfflineHabitSnapshot snapshot, Guid operationId) =>
        await js.InvokeVoidAsync("localStorage.removeItem", PendingPrefix(snapshot) + operationId.ToString("D"));
}
