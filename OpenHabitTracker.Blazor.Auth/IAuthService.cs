namespace OpenHabitTracker.Blazor.Auth;

public interface IAuthService
{
    string? Error { get; set; }

    bool IsSignedOut { get; }

    bool CanRefreshTokenLogin { get; }

    Task<bool> CredentialsLogin(string address, string username, string password);

    Task<bool> RefreshTokenLogin(string address, string refreshToken);

    Task<bool> TryRefreshTokenLogin();

    Task Logout();

    Task SignedOut();
}
