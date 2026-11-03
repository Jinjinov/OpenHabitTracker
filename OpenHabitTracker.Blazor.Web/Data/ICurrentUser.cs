namespace OpenHabitTracker.Blazor.Web.Data;

public interface ICurrentUser
{
    // Zero when nobody is signed in, and outside both a request and a circuit, such as at startup.
    Task<long> GetUserId();

    Task<bool> IsAdministrator();
}
