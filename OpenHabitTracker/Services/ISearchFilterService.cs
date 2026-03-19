using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.Services;

public interface ISearchFilterService
{
    string? SearchTerm { get; set; }
    bool MatchCase { get; set; }
    DateTime? DoneAtFilter { get; set; }
    DateCompare DoneAtCompare { get; set; }
    DateTime? PlannedAtFilter { get; set; }
    DateCompare PlannedAtCompare { get; set; }
    QueryParameters GetQueryParameters(SettingsModel settings);
    string MarkSearchResults(string text);
    string MarkSearchResultsInHtml(string text);
}
