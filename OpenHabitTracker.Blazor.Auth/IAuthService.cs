namespace OpenHabitTracker.Blazor.Auth;

public interface IAuthService
{
    string? Login { get; set; }

    string? Error { get; set; }

    Task<bool> CredentialsLogin(string address, string username, string password);

    Task<bool> RefreshTokenLogin(string address, string refreshToken);

    Task Logout();
}
