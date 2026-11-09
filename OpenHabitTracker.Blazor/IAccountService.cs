using OpenHabitTracker.Dto;

namespace OpenHabitTracker.Blazor;

// The account on the server: the device apps reach it with their token, the browser on the server with its cookie.
// Each call that can fail returns its errors, and none when it succeeded.
public interface IAccountService
{
    Task<AccountResponse?> GetAccount();

    Task<IReadOnlyList<string>> ChangePassword(string currentPassword, string newPassword);

    Task LogOut();

    Task<IReadOnlyList<AccountResponse>> GetAccounts();

    Task<IReadOnlyList<string>> CreateAccount(string username, string email, string password);

    Task<IReadOnlyList<string>> ResetPassword(string username, string password);
}
