using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Dto;
using System.Text.Json;

namespace OpenHabitTracker.Blazor.Web;

// Calls the account API from the browser with its cookie, because a password change renews the cookie
// and a log out clears it, and only a response to the browser can do either.
public sealed class BrowserAccountService(IJSRuntime jsRuntime, NavigationManager navigationManager, IStringLocalizer loc) : IAccountService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./account.js").AsTask());

    public async Task<AccountResponse?> GetAccount()
    {
        BrowserResponse response = await Send("GET", "api/account");

        return response.Status == 200 ? JsonSerializer.Deserialize<AccountResponse>(response.Body, _jsonOptions) : null;
    }

    public async Task<IReadOnlyList<string>> ChangePassword(string currentPassword, string newPassword)
    {
        return Errors(await Send("POST", "api/account/password", new { CurrentPassword = currentPassword, NewPassword = newPassword }));
    }

    public async Task LogOut()
    {
        await Send("POST", "api/account/logout", new { });

        navigationManager.NavigateTo("/login", forceLoad: true);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAccounts()
    {
        BrowserResponse response = await Send("GET", "api/account/accounts");

        return response.Status == 200 ? JsonSerializer.Deserialize<List<AccountResponse>>(response.Body, _jsonOptions) ?? [] : [];
    }

    public async Task<IReadOnlyList<string>> CreateAccount(string username, string email, string password)
    {
        return Errors(await Send("POST", "api/account/accounts", new { Username = username, Email = email, Password = password }));
    }

    public async Task<IReadOnlyList<string>> ResetPassword(string username, string password)
    {
        return Errors(await Send("POST", "api/account/accounts/password", new { Username = username, Password = password }));
    }

    private IReadOnlyList<string> Errors(BrowserResponse response)
    {
        return response.Status == 200 ? [] : AccountErrors.From(response.Status, response.Body, loc);
    }

    private async Task<BrowserResponse> Send(string method, string url, object? body = null)
    {
        IJSObjectReference module = await _moduleTask.Value;

        return await module.InvokeAsync<BrowserResponse>("send", method, url, body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            try
            {
                IJSObjectReference module = await _moduleTask.Value;

                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private sealed class BrowserResponse
    {
        public int Status { get; set; }

        public string Body { get; set; } = string.Empty;
    }
}
