using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.App;

public class MergeOptions
{
    // The source records only the day of a completion, at midday, so a completion is added only when its
    // habit has none on that day, and a day already done in this app is not done twice.
    public bool MatchCompletionsByDay { get; init; }

    // Categories that are hidden from the lists when the import creates them, such as archived habits.
    public IReadOnlyCollection<CategoryModel> HiddenCategories { get; init; } = [];

    // The content an earlier version of the importer gave a note, so a note imported by that version
    // matches instead of being added again.
    public IReadOnlyDictionary<NoteModel, string> FormerContents { get; init; } = new Dictionary<NoteModel, string>();
}
