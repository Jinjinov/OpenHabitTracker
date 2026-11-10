using HtmlAgilityPack;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenHabitTracker.Services;

public class SearchFilterService : ISearchFilterService
{
    public string? SearchTerm { get; set; }

    public bool MatchCase { get; set; }

    public DateTime? DoneAtFilter { get; set; }

    public DateCompare DoneAtCompare { get; set; } = DateCompare.On;

    public DateTime? PlannedAtFilter { get; set; }

    public DateCompare PlannedAtCompare { get; set; } = DateCompare.On;

    public QueryParameters GetQueryParameters(SettingsModel settings)
    {
        DateTime today = DateTime.Today;

        (DateTime? plannedRangeStart, DateTime? plannedRangeEnd) = settings.ResolvePlannedRange(today);
        (DateTime? doneRangeStart, DateTime? doneRangeEnd) = settings.ResolveDoneRange(today);

        return new()
        {
            SearchTerm = SearchTerm,
            MatchCase = MatchCase,
            DoneAtFilter = DoneAtFilter,
            DoneAtCompare = DoneAtCompare,
            PlannedAtFilter = PlannedAtFilter,
            PlannedAtCompare = PlannedAtCompare,
            PlannedRangeStart = plannedRangeStart,
            PlannedRangeEnd = plannedRangeEnd,
            DoneRangeStart = doneRangeStart,
            DoneRangeEnd = doneRangeEnd,
            ShowDoneInRange = settings.ShowDoneInRange,
            HideCompletedTasks = settings.HideCompletedTasks,
            ShowOnlyOverSelectedRatioMin = settings.ShowOnlyOverSelectedRatioMin,
            ShowOnlyUnderSelectedRatioMax = settings.ShowOnlyUnderSelectedRatioMax,
            SelectedRatioMin = settings.SelectedRatioMin,
            SelectedRatioMax = settings.SelectedRatioMax,
            SelectedRatio = settings.SelectedRatio,
            CategoryFilterDisplay = settings.CategoryFilterDisplay,
            PriorityFilterDisplay = settings.PriorityFilterDisplay,
            SelectedCategoryId = settings.SelectedCategoryId,
            SelectedPriority = settings.SelectedPriority,
            HiddenCategoryIds = settings.HiddenCategoryIds,
            ShowPriority = settings.ShowPriority,
            SortBy = settings.SortBy,
        };
    }

    public string MarkSearchResults(string text)
    {
        if (string.IsNullOrEmpty(SearchTerm))
            return text;

        if (MatchCase)
            return text.Replace(SearchTerm, $"<mark>{SearchTerm}</mark>");
        else
            return MarkSearchResults(text, "<mark>", "</mark>");
    }

    string MarkSearchResults(string input, string before, string after)
    {
        // Create a pattern to match the search term with case-insensitivity
        string pattern = "(?i)" + Regex.Escape(SearchTerm!);

        // Replace the search term with the marked version
        return Regex.Replace(input, pattern, match =>
        {
            // Get the matched search term with its original case
            string matchedTerm = match.Value;

            // Add mark tag to the start and end of the matched term
            return $"{before}{matchedTerm}{after}";
        });
    }

    public string MarkSearchResultsInHtml(string text)
    {
        if (string.IsNullOrEmpty(SearchTerm))
            return text;

        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(text);

        HtmlNodeCollection coll = htmlDoc.DocumentNode.SelectNodes("//text()");

        if (coll is null)
            return text;

        StringComparison comparisonType = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        // The text of a node holds & < > " as entities, so the term is matched in the decoded text,
        // or a term like "a" would split "&amp;" and show it as written.
        foreach (HtmlTextNode node in coll.Cast<HtmlTextNode>())
        {
            string decoded = HtmlEntity.DeEntitize(node.Text);

            int index = decoded.IndexOf(SearchTerm, comparisonType);

            if (index < 0)
                continue;

            StringBuilder marked = new();
            int start = 0;

            while (index >= 0)
            {
                marked.Append(WebUtility.HtmlEncode(decoded[start..index]));
                marked.Append("<mark>").Append(WebUtility.HtmlEncode(decoded.Substring(index, SearchTerm.Length))).Append("</mark>");

                start = index + SearchTerm.Length;
                index = decoded.IndexOf(SearchTerm, start, comparisonType);
            }

            marked.Append(WebUtility.HtmlEncode(decoded[start..]));

            node.Text = marked.ToString();
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }
}
