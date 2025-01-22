namespace OpenHabitTracker.Data.Models;

public class UserModel
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
