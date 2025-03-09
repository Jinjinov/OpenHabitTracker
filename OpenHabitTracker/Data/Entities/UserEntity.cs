namespace OpenHabitTracker.Data.Entities;

public class UserEntity : IUserEntity
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime LastChangeAt { get; set; }
}
