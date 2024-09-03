using HtmlAgilityPack;
using OpenHabitTracker.Data;
using System.Text.RegularExpressions;

namespace OpenHabitTracker.Services;

public class SearchFilterService
{
    public string? SearchTerm { get; set; }

    public bool MatchCase { get; set; }

    public DateTime? DoneAtFilter { get; set; }

    public DateCompare DoneAtCompare { get; set; } = DateCompare.On;

    public DateTime? PlannedAtFilter { get; set; }

    public DateCompare PlannedAtCompare { get; set; } = DateCompare.On;

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

        if (MatchCase)
        {
            foreach (HtmlTextNode node in coll.Cast<HtmlTextNode>())
            {
                node.Text = node.Text.Replace(SearchTerm, $"<mark>{SearchTerm}</mark>");
            }
        }
        else
        {
            foreach (HtmlTextNode node in coll.Cast<HtmlTextNode>())
            {
                node.Text = MarkSearchResults(node.Text, "<mark>", "</mark>");
            }
        }

        return htmlDoc.DocumentNode.OuterHtml;
    }
}
