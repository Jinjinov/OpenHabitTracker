namespace OpenHabitTracker.App;

// The startup crash handler's only output. It throws rather than swallowing, because the caller
// is already inside a catch-everything handler and the self-test needs the failure to surface.
public static class CrashLog
{
    public const string FileName = "Error.log";

    public static void Write(string directory, string? message, string fileName = FileName) =>
        File.WriteAllText(Path.Combine(directory, fileName), message);

    public static string? Read(string directory, string fileName = FileName)
    {
        string path = Path.Combine(directory, fileName);

        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
