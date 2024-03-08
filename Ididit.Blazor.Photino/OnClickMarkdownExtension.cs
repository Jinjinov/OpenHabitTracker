using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Ididit.Blazor.Photino;

public class OnClickMarkdownExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += AddOnClickToLinks;
    }

    public void AddOnClickToLinks(MarkdownDocument document)
    {
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            link.GetAttributes().AddProperty("onclick", $"return openLink('{link.Url}');");
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}
