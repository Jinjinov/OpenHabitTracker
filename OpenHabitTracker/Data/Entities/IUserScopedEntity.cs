namespace OpenHabitTracker.Data.Entities;

// A row that belongs to one account. The server filters and stamps by it; the local hosts leave it at zero.
public interface IUserScopedEntity
{
    long UserId { get; set; }
}
