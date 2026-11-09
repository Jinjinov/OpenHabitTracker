using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor.Auth;

// Calls the account API of the server the device syncs with.
public class ApiAccountService(AccountClient accountClient, ApiClientOptions apiClientOptions, IAuthService authService, IStringLocalizer loc) : IAccountService
{
    private readonly AccountClient _accountClient = accountClient;
    private readonly ApiClientOptions _apiClientOptions = apiClientOptions;
    private readonly IAuthService _authService = authService;
    private readonly IStringLocalizer _loc = loc;

    public async Task<AccountResponse?> GetAccount()
    {
        try
        {
            return await _accountClient.GetAccountAsync();
        }
        catch (Exception exception) when (IsCallFailure(exception))
        {
            return null;
        }
    }

    // The device's own refresh token goes along, so the server keeps this device signed in.
    public async Task<IReadOnlyList<string>> ChangePassword(string currentPassword, string newPassword)
    {
        return await Call(() => _accountClient.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            RefreshToken = _apiClientOptions.RefreshToken
        }));
    }

    // A device that cannot reach the server still logs out; the server then keeps its token until it expires.
    public async Task LogOut()
    {
        try
        {
            await _accountClient.LogOutAsync(new LogOutRequest { RefreshToken = _apiClientOptions.RefreshToken });
        }
        catch (Exception exception) when (IsCallFailure(exception))
        {
        }

        await _authService.Logout();
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAccounts()
    {
        try
        {
            return await _accountClient.GetAccountsAsync();
        }
        catch (Exception exception) when (IsCallFailure(exception))
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> CreateAccount(string username, string email, string password)
    {
        return await Call(() => _accountClient.CreateAccountAsync(new CreateAccountRequest { Username = username, Email = email, Password = password }));
    }

    public async Task<IReadOnlyList<string>> ResetPassword(string username, string password)
    {
        return await Call(() => _accountClient.ResetPasswordAsync(new ResetPasswordRequest { Username = username, Password = password }));
    }

    private async Task<IReadOnlyList<string>> Call(Func<Task> call)
    {
        try
        {
            await call();

            return [];
        }
        catch (ApiException exception)
        {
            return AccountErrors.From(exception.StatusCode, exception.Response, _loc);
        }
        catch (Exception exception) when (IsCallFailure(exception))
        {
            return [_loc["Connection error"]];
        }
    }

    private static bool IsCallFailure(Exception exception)
    {
        return exception is ApiException or SignedOutException or HttpRequestException or TaskCanceledException or InvalidOperationException;
    }
}
