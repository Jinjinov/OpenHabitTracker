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
        // Attach a delegate to the DocumentProcessed event
        pipeline.DocumentProcessed += ParserLinkInlines;
    }

    public void ParserLinkInlines(MarkdownDocument document)
    {
        // This method is called after the document has been parsed.
        // You can use it to traverse and manipulate the DOM.
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            var attributes = link.GetAttributes();
            attributes.AddProperty("onclick", "alert('Hi'); return false;");
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}
