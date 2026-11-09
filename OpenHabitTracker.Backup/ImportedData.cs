using OpenHabitTracker.App;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Backup;

// What an importer of another app's file read, and how it is merged.
public record ImportedData(List<CategoryModel> Categories, MergeOptions Options);
