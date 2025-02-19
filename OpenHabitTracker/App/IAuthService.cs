namespace OpenHabitTracker.App;

public interface IAuthService
{
    string? Login { get; set; }

    string? Error { get; set; }

    Task<bool> CredentialsLogin(string address, string username, string password);

    Task<bool> RefreshTokenLogin(string address, string refreshToken);

    void Logout();
}
