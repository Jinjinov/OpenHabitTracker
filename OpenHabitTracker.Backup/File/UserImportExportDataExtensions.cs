using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Backup.File;

internal static class UserImportExportDataExtensions
{
    // A backup writes "Tasks": null (JSON) or an empty "Tasks:" (YAML) for a category without tasks,
    // and the same for notes and habits; the deserializers set those lists to null.
    internal static UserImportExportData WithEmptyListsForNull(this UserImportExportData data)
    {
        foreach (CategoryModel category in data.Categories)
        {
            category.Notes ??= [];
            category.Tasks ??= [];
            category.Habits ??= [];
        }

        return data;
    }
}
