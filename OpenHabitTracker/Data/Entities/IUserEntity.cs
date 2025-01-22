namespace OpenHabitTracker.Data.Entities;

public interface IUserEntity
{
    long Id { get; set; }

    string UserName { get; set; }

    string Email { get; set; }

    string PasswordHash { get; set; }
}
