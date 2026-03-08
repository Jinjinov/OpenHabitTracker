using OpenHabitTracker.Data;

namespace OpenHabitTracker.Services;

public interface ISearchFilterService
{
    string? SearchTerm { get; set; }
    bool MatchCase { get; set; }
    DateTime? DoneAtFilter { get; set; }
    DateCompare DoneAtCompare { get; set; }
    DateTime? PlannedAtFilter { get; set; }
    DateCompare PlannedAtCompare { get; set; }
    string MarkSearchResults(string text);
    string MarkSearchResultsInHtml(string text);
}
