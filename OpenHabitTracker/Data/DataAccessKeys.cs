namespace OpenHabitTracker.Data;

// Two IDataAccess implementations are registered, and anything below ClientState has to name the one
// it means: asking for the collection would construct the remote one too.
public static class DataAccessKeys
{
    public const string Local = "local";
}
