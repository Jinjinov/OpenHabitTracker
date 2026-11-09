using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenHabitTracker.Backup.GoogleKeep;

// Keep exports a note's formatting only as HTML: one block per line, h1 and h2 for headings, and spans whose inline style carries bold, italic and underline.
internal static partial class GoogleKeepMarkdown
{
    private readonly record struct Style(bool Bold, bool Italic, bool Underline);

    private readonly record struct Run(string Text, Style Style);

    [GeneratedRegex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)([^>]*)>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"style\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex StyleAttribute();

    [GeneratedRegex(@"font-weight\s*:\s*(bold|[6-9]00)", RegexOptions.IgnoreCase)]
    private static partial Regex BoldStyle();

    [GeneratedRegex(@"font-style\s*:\s*italic", RegexOptions.IgnoreCase)]
    private static partial Regex ItalicStyle();

    [GeneratedRegex(@"text-decoration\s*:[^;]*underline", RegexOptions.IgnoreCase)]
    private static partial Regex UnderlineStyle();

    internal static string FromHtml(string html)
    {
        List<string> lines = [];
        List<Run> runs = [];
        Stack<Style> styles = new();
        string prefix = string.Empty;
        bool blockHasLine = false;

        void EndLine()
        {
            lines.Add(prefix + ToMarkdown(runs));
            runs.Clear();
            blockHasLine = true;
        }

        void AddText(string text)
        {
            if (text.Length > 0)
                runs.Add(new(WebUtility.HtmlDecode(text), styles.TryPeek(out Style style) ? style : default));
        }

        int position = 0;

        foreach (Match match in Tag().Matches(html))
        {
            AddText(html[position..match.Index]);
            position = match.Index + match.Length;

            bool isClosing = match.Groups[1].Value == "/";
            string name = match.Groups[2].Value.ToLowerInvariant();
            string attributes = match.Groups[3].Value;

            if (name == "br")
            {
                EndLine();
            }
            else if (name is "p" or "div" or "li" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
            {
                if (isClosing)
                {
                    if (runs.Count > 0 || !blockHasLine)
                        EndLine();
                }
                else
                {
                    // Whitespace between blocks is HTML layout, not a line of the note.
                    if (runs.Any(run => !string.IsNullOrWhiteSpace(run.Text)))
                        EndLine();

                    runs.Clear();
                    blockHasLine = false;
                }

                prefix = !isClosing && name[0] == 'h' ? new string('#', name[1] - '0') + " " : string.Empty;
            }
            else if (name is "span" or "b" or "strong" or "i" or "em" or "u")
            {
                if (isClosing)
                {
                    styles.TryPop(out _);
                }
                else if (!attributes.TrimEnd().EndsWith('/'))
                {
                    styles.Push(GetStyle(name, attributes, styles.TryPeek(out Style parent) ? parent : default));
                }
            }
        }

        AddText(html[position..]);

        if (runs.Any(run => !string.IsNullOrWhiteSpace(run.Text)))
            EndLine();

        return string.Join("\n", lines).TrimEnd();
    }

    private static Style GetStyle(string name, string attributes, Style parent)
    {
        Style style = name switch
        {
            "b" or "strong" => parent with { Bold = true },
            "i" or "em" => parent with { Italic = true },
            "u" => parent with { Underline = true },
            _ => parent
        };

        Match styleAttribute = StyleAttribute().Match(attributes);

        if (!styleAttribute.Success)
            return style;

        string css = styleAttribute.Groups[1].Value;

        // Keep writes every property on every span, so font-weight:400 or text-decoration:none turns an inherited style off.
        if (css.Contains("font-weight", StringComparison.OrdinalIgnoreCase))
            style = style with { Bold = BoldStyle().IsMatch(css) };

        if (css.Contains("font-style", StringComparison.OrdinalIgnoreCase))
            style = style with { Italic = ItalicStyle().IsMatch(css) };

        if (css.Contains("text-decoration", StringComparison.OrdinalIgnoreCase))
            style = style with { Underline = UnderlineStyle().IsMatch(css) };

        return style;
    }

    private static string ToMarkdown(List<Run> runs)
    {
        // Keep splits a line into one span per style change, and neighbouring spans can share a style.
        List<Run> merged = [];

        foreach (Run run in runs)
        {
            if (merged.Count > 0 && merged[^1].Style == run.Style)
                merged[^1] = merged[^1] with { Text = merged[^1].Text + run.Text };
            else
                merged.Add(run);
        }

        StringBuilder markdown = new();

        foreach (Run run in merged)
        {
            string core = run.Text.Trim();

            if (core.Length == 0 || run.Style == default)
            {
                markdown.Append(run.Text);
                continue;
            }

            // Markdown emphasis does not open before or close after a space, so the spaces go outside the markers.
            string opening = (run.Style.Bold ? "**" : "") + (run.Style.Italic ? "*" : "") + (run.Style.Underline ? "++" : "");
            string closing = (run.Style.Underline ? "++" : "") + (run.Style.Italic ? "*" : "") + (run.Style.Bold ? "**" : "");

            int leading = run.Text.Length - run.Text.TrimStart().Length;

            markdown.Append(run.Text[..leading]);
            markdown.Append(opening).Append(core).Append(closing);
            markdown.Append(run.Text[(leading + core.Length)..]);
        }

        return markdown.ToString();
    }
}
