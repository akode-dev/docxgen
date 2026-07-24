using Akode.DocxGen.Core.Markdown;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplater;
using DocxTemplater.Formatter;

namespace Akode.DocxGen.Docx.Rendering;

internal sealed class DocxGenMarkdownFormatter : IFormatter
{
    public bool CanHandle(Type valueType, string formatter) =>
        typeof(MarkdownContent).IsAssignableFrom(valueType)
        && string.Equals(formatter, "MD", StringComparison.OrdinalIgnoreCase);

    public void ApplyFormat(
        ITemplateProcessingContext context,
        FormatterContext formatterContext,
        Text target)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(formatterContext);
        ArgumentNullException.ThrowIfNull(target);
        if (formatterContext.Value is not MarkdownContent content)
        {
            throw new InvalidOperationException(
                "The MD formatter requires renderer-neutral MarkdownContent.");
        }

        var paragraph = target.Ancestors<Paragraph>().FirstOrDefault()
            ?? throw new InvalidDataException(
                "A Markdown placeholder must be contained by a paragraph.");
        var sectionProperties = paragraph.ParagraphProperties?
            .GetFirstChild<SectionProperties>()?
            .CloneNode(deep: true);
        var renderer = new OpenXmlMarkdownRenderer(context.MainDocumentPart);
        var rendered = renderer.Render(content).ToList();
        if (rendered.Count == 0)
        {
            rendered.Add(new Paragraph(new Run(new Text(string.Empty))));
        }

        if (sectionProperties is not null
            && rendered[^1] is Paragraph lastParagraph)
        {
            lastParagraph.ParagraphProperties ??= new ParagraphProperties();
            lastParagraph.ParagraphProperties.AppendChild(sectionProperties);
        }

        foreach (var element in rendered)
        {
            paragraph.InsertBeforeSelf(element);
        }

        paragraph.Remove();
    }
}
