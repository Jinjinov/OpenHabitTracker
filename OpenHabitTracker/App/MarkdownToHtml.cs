using Markdig;

namespace OpenHabitTracker.App;

public class MarkdownToHtml(MarkdownPipeline markdownPipeline)
{
    private readonly MarkdownPipeline _markdownPipeline = markdownPipeline;

    public string GetMarkdown(string content)
    {
        //return Settings.DisplayNoteContentAsMarkdown ? Markdown.ToHtml(content, _markdownPipeline) : content;
        return Markdown.ToHtml(content, _markdownPipeline);
    }
}
