namespace OpenHabitTracker.App;

public interface IAuthService
{
    string? Login { get; set; }

    string? Error { get; set; }

    Task<string?> CredentialsLogin(string address, string username, string password);

    Task<string?> RefreshTokenLogin(string address, string refreshToken);

    void Logout();
}
