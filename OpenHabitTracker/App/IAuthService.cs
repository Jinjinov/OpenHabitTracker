namespace OpenHabitTracker.App;

public interface IAuthService
{
    Task<(string? Login, string? Error)> Login(string address, string username, string password);

    Task<(string? Login, string? Error)> Login(string address, string refreshToken);

    void Logout();
}
