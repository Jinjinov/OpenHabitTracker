using OpenHabitTracker.App;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Backup;

// A file that was read and waits for the import to be confirmed.
public class PendingImport
{
    public required ImportFormat Format { get; init; }

    public required string Filename { get; init; }

    public required List<CategoryModel> Categories { get; init; }

    public MergeOptions Options { get; init; } = new();

    // The settings the file carries, which replace the current ones.
    public SettingsModel? Settings { get; init; }

    public required ImportCounts Found { get; init; }

    public required ImportCounts New { get; init; }
}
