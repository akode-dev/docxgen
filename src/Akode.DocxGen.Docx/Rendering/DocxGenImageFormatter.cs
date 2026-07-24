using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Model;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplater;
using DocxTemplater.Formatter;

namespace Akode.DocxGen.Docx.Rendering;

internal sealed class DocxGenImageFormatter : IFormatter
{
    public bool CanHandle(Type valueType, string formatter) =>
        typeof(ResolvedAsset).IsAssignableFrom(valueType)
        && string.Equals(formatter, "IMG", StringComparison.OrdinalIgnoreCase);

    public void ApplyFormat(
        ITemplateProcessingContext context,
        FormatterContext formatterContext,
        Text target)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(formatterContext);
        ArgumentNullException.ThrowIfNull(target);
        if (formatterContext.Value is not ResolvedAsset asset)
        {
            throw new InvalidOperationException(
                "The IMG formatter requires a resolved $file asset.");
        }

        if (!asset.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"The IMG asset '{asset.FullPath}' is not a supported image.");
        }

        var paragraph = target.Ancestors<Paragraph>().FirstOrDefault()
            ?? throw new InvalidDataException(
                "An IMG placeholder must be contained by a paragraph.");
        var sectionProperties = paragraph.ParagraphProperties?
            .GetFirstChild<SectionProperties>()?
            .CloneNode(deep: true);
        var alt = GetAltText(formatterContext, asset);
        var content = new MarkdownContent(
            [
                new MarkdownParagraphNode(
                    [new MarkdownImageNode(asset, alt, Title: null)]),
            ],
            new MarkdownStats(1, 0, 0, 1, 0),
            []);
        var renderer = new OpenXmlMarkdownRenderer(context.MainDocumentPart);
        var rendered = renderer.Render(content).ToList();
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

    private static string GetAltText(
        FormatterContext context,
        ResolvedAsset asset)
    {
        var explicitAlt = context.Args.FirstOrDefault(
            argument => argument.StartsWith("alt=", StringComparison.OrdinalIgnoreCase));
        if (explicitAlt is not null)
        {
            return explicitAlt[4..].Trim().Trim('"');
        }

        return Uri.TryCreate(asset.FullPath, UriKind.Absolute, out var uri)
               && uri.IsAbsoluteUri
            ? Path.GetFileName(uri.LocalPath)
            : Path.GetFileName(asset.FullPath);
    }
}
