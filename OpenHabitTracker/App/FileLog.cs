using Microsoft.Extensions.Logging;
using System.Text;

namespace OpenHabitTracker.App;

// The one log file of every host that has a data directory, next to the db as Error.log.
// Static, because the crash handlers and the update check write here with no service provider;
// FileLoggerProvider is a thin ILogger over the same writer.
// Appends, and drops the oldest half once the file passes MaxBytes, so it can be sent along with a
// bug report without ever growing past a megabyte.
public static class FileLog
{
    public const string FileName = "Error.log";

    // A few lines per session make this months of history.
    public const long MaxBytes = 1024 * 1024;

    private static readonly object _lock = new();

    // Never throws: a log that cannot be written must not take the app down with it.
    public static void Write(string directory, LogLevel level, string category, string message, Exception? exception = null, string fileName = FileName)
    {
        try
        {
            StringBuilder entry = new();

            entry.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" [").Append(level).Append("] ").Append(category).Append(": ").Append(message);

            if (exception is not null)
                entry.AppendLine().Append(exception);

            Append(directory, fileName, entry.ToString());
        }
        catch
        {
        }
    }

    // The raw write, which throws: the self-test needs a failed write to surface.
    public static void Append(string directory, string fileName, string entry)
    {
        string path = Path.Combine(directory, fileName);

        lock (_lock)
        {
            File.AppendAllText(path, entry + Environment.NewLine);

            Trim(path);
        }
    }

    public static string? Read(string directory, string fileName = FileName)
    {
        string path = Path.Combine(directory, fileName);

        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    // Keeps the newest half, cut at a line boundary, so the file settles between half the cap and the cap.
    private static void Trim(string path)
    {
        if (new FileInfo(path).Length <= MaxBytes)
            return;

        string text = File.ReadAllText(path);

        int cut = text.IndexOf('\n', text.Length / 2);

        if (cut < 0)
            return;

        File.WriteAllText(path, text[(cut + 1)..]);
    }
}
