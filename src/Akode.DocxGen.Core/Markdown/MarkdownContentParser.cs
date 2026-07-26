using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Core.Security;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Akode.DocxGen.Core.Markdown;

/// <summary>Parses the supported Markdown subset into an immutable neutral model.</summary>
public static class MarkdownContentParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Parses one Markdown fragment using the supplied security policy.</summary>
    public static MarkdownContent Parse(
        string markdown,
        RenderOptions options,
        IAssetResolver assetResolver)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(assetResolver);

        var diagnostics = new DiagnosticCollector();
        var context = new ParseContext(options, assetResolver, diagnostics);
        var document = Markdig.Markdown.Parse(markdown, Pipeline);
        var blocks = ConvertBlocks(document, context);
        return new MarkdownContent(
            blocks,
            new MarkdownStats(
                Sections: 1,
                context.HeadingCount,
                context.TableCount,
                context.ImageCount,
                context.CodeBlockCount),
            diagnostics.Items);
    }

    private static List<MarkdownBlockNode> ConvertBlocks(
        ContainerBlock container,
        ParseContext context)
    {
        var result = new List<MarkdownBlockNode>();
        foreach (var block in container)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    var requestedLevel = heading.Level + context.Options.HeadingOffset;
                    var level = Math.Clamp(requestedLevel, 1, 6);
                    if (level != requestedLevel)
                    {
                        context.Diagnostics.Add(
                            DiagnosticCode.HeadingLevelClamped,
                            message:
                            $"Markdown heading level {heading.Level} with offset "
                            + $"{context.Options.HeadingOffset} was clamped to Heading {level}.");
                    }

                    context.HeadingCount++;
                    result.Add(new MarkdownHeadingNode(
                        level,
                        ConvertInlines(heading.Inline, context)));
                    break;

                case ParagraphBlock paragraph:
                    result.Add(new MarkdownParagraphNode(
                        ConvertInlines(paragraph.Inline, context)));
                    break;

                case FencedCodeBlock fenced:
                    context.CodeBlockCount++;
                    result.Add(new MarkdownCodeBlockNode(
                        fenced.Lines.ToString(),
                        string.IsNullOrWhiteSpace(fenced.Info)
                            ? null
                            : fenced.Info.ToString()));
                    break;

                case CodeBlock code:
                    context.CodeBlockCount++;
                    result.Add(new MarkdownCodeBlockNode(code.Lines.ToString(), null));
                    break;

                case QuoteBlock quote:
                    result.Add(new MarkdownQuoteNode(ConvertBlocks(quote, context)));
                    break;

                case ListBlock list:
                    result.Add(ConvertList(list, context));
                    break;

                case Table table:
                    context.TableCount++;
                    result.Add(ConvertTable(table, context));
                    break;

                case ThematicBreakBlock:
                    result.Add(new MarkdownHorizontalRuleNode());
                    break;

                case HtmlBlock html:
                    HandleHtml(html.Lines.ToString(), result, context);
                    break;

                default:
                    context.Diagnostics.Add(
                        DiagnosticCode.MarkdownFeatureDowngraded,
                        message: $"Markdown block '{block.GetType().Name}' was omitted.");
                    break;
            }
        }

        return result;
    }

    private static MarkdownListNode ConvertList(ListBlock list, ParseContext context)
    {
        var items = new List<MarkdownListItemNode>();
        foreach (var item in list.OfType<ListItemBlock>())
        {
            items.Add(new MarkdownListItemNode(ConvertBlocks(item, context)));
        }

        var start = 1;
        if (list.IsOrdered
            && !string.IsNullOrWhiteSpace(list.OrderedStart)
            && int.TryParse(
                list.OrderedStart,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedStart))
        {
            start = parsedStart;
        }

        return new MarkdownListNode(list.IsOrdered, start, items);
    }

    private static MarkdownTableNode ConvertTable(Table table, ParseContext context)
    {
        IReadOnlyList<MarkdownInlineNode>[] header = [];
        var rows = new List<IReadOnlyList<IReadOnlyList<MarkdownInlineNode>>>();

        foreach (var row in table.OfType<TableRow>())
        {
            var cells = row
                .OfType<TableCell>()
                .Select(cell => CellInlines(cell, context))
                .ToArray();
            if (row.IsHeader && header.Length == 0)
            {
                header = cells;
            }
            else
            {
                rows.Add(cells);
            }
        }

        return new MarkdownTableNode(header, rows);
    }

    private static List<MarkdownInlineNode> CellInlines(
        TableCell cell,
        ParseContext context)
    {
        var result = new List<MarkdownInlineNode>();
        foreach (var paragraph in cell.OfType<ParagraphBlock>())
        {
            if (result.Count > 0)
            {
                result.Add(new MarkdownBreakNode(Hard: true));
            }

            result.AddRange(ConvertInlines(paragraph.Inline, context));
        }

        return result;
    }

    private static List<MarkdownInlineNode> ConvertInlines(
        ContainerInline? container,
        ParseContext context)
    {
        if (container is null)
        {
            return [];
        }

        var result = new List<MarkdownInlineNode>();
        for (var inline = container.FirstChild;
             inline is not null;
             inline = inline.NextSibling)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    result.Add(new MarkdownTextNode(literal.Content.ToString()));
                    break;

                case LineBreakInline lineBreak:
                    result.Add(new MarkdownBreakNode(lineBreak.IsHard));
                    break;

                case CodeInline code:
                    result.Add(new MarkdownCodeInlineNode(code.Content));
                    break;

                case TaskList taskList:
                    result.Add(new MarkdownTaskListNode(taskList.Checked));
                    break;

                case EmphasisInline emphasis:
                    result.Add(new MarkdownEmphasisNode(
                        Bold: emphasis.DelimiterChar != '~' && emphasis.DelimiterCount >= 2,
                        Italic: emphasis.DelimiterChar != '~' && emphasis.DelimiterCount == 1,
                        Strikethrough: emphasis.DelimiterChar == '~',
                        ConvertInlines(emphasis, context)));
                    break;

                case LinkInline link when link.IsImage:
                    AddImage(link, result, context);
                    break;

                case LinkInline link:
                    result.Add(new MarkdownLinkNode(
                        CreateUri(link.Url),
                        link.Title,
                        ConvertInlines(link, context)));
                    break;

                case AutolinkInline autoLink:
                    var text = autoLink.IsEmail ? $"mailto:{autoLink.Url}" : autoLink.Url;
                    result.Add(new MarkdownLinkNode(
                        CreateUri(text),
                        null,
                        [new MarkdownTextNode(autoLink.Url)]));
                    break;

                case HtmlInline html:
                    HandleInlineHtml(html.Tag, result, context);
                    break;

                default:
                    context.Diagnostics.Add(
                        DiagnosticCode.MarkdownFeatureDowngraded,
                        message: $"Markdown inline '{inline.GetType().Name}' was omitted.");
                    break;
            }
        }

        return result;
    }

    private static void AddImage(
        LinkInline link,
        List<MarkdownInlineNode> result,
        ParseContext context)
    {
        var source = link.Url ?? string.Empty;
        var alt = PlainText(link);
        context.ImageCount++;
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https")
        {
            AddRemoteImage(uri, alt, link.Title, result, context);
            return;
        }

        try
        {
            var asset = context.AssetResolver.Resolve(
                Uri.UnescapeDataString(source.Replace('/', Path.DirectorySeparatorChar)),
                AssetKind.Image);
            result.Add(new MarkdownImageNode(asset, alt, link.Title));
        }
        catch (AssetResolutionException exception)
        {
            context.Diagnostics.Add(exception.Diagnostic);
            result.Add(new MarkdownTextNode(
                string.IsNullOrWhiteSpace(alt) ? $"[{source}]" : alt));
        }
    }

    private static void AddRemoteImage(
        Uri uri,
        string alt,
        string? title,
        List<MarkdownInlineNode> result,
        ParseContext context)
    {
        if (!context.Options.AllowRemoteImages)
        {
            context.Diagnostics.Add(DiagnosticCode.RemoteImageBlocked, uri.AbsoluteUri);
            result.Add(new MarkdownTextNode(
                string.IsNullOrWhiteSpace(alt) ? $"[{uri}]" : alt));
            return;
        }

        if (context.AssetResolver is not IRemoteAssetResolver remoteResolver)
        {
            context.Diagnostics.Add(
                DiagnosticCode.RemoteImageDownloadFailed,
                uri.AbsoluteUri,
                "The configured asset resolver does not support remote images.");
            result.Add(new MarkdownTextNode(
                string.IsNullOrWhiteSpace(alt) ? $"[{uri}]" : alt));
            return;
        }

        try
        {
            var asset = remoteResolver.ResolveRemote(uri, AssetKind.Image);
            result.Add(new MarkdownImageNode(asset, alt, title));
        }
        catch (AssetResolutionException exception)
        {
            context.Diagnostics.Add(exception.Diagnostic);
            result.Add(new MarkdownTextNode(
                string.IsNullOrWhiteSpace(alt) ? $"[{uri}]" : alt));
        }
    }

    private static string PlainText(ContainerInline container)
    {
        var builder = new System.Text.StringBuilder();
        for (var inline = container.FirstChild;
             inline is not null;
             inline = inline.NextSibling)
        {
            if (inline is LiteralInline literal)
            {
                builder.Append(literal.Content);
            }
            else if (inline is CodeInline code)
            {
                builder.Append(code.Content);
            }
            else if (inline is ContainerInline nested)
            {
                builder.Append(PlainText(nested));
            }
        }

        return builder.ToString();
    }

    private static void HandleHtml(
        string html,
        List<MarkdownBlockNode> result,
        ParseContext context)
    {
        if (!context.Options.AllowRawHtml)
        {
            context.Diagnostics.Add(DiagnosticCode.RawHtmlStripped);
            return;
        }

        result.Add(new MarkdownParagraphNode([new MarkdownTextNode(html)]));
    }

    private static void HandleInlineHtml(
        string html,
        List<MarkdownInlineNode> result,
        ParseContext context)
    {
        if (!context.Options.AllowRawHtml)
        {
            context.Diagnostics.Add(DiagnosticCode.RawHtmlStripped);
            return;
        }

        result.Add(new MarkdownTextNode(html));
    }

    private static Uri CreateUri(string? value) =>
        Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri)
            ? uri
            : new Uri("#", UriKind.Relative);

    private sealed class ParseContext(
        RenderOptions options,
        IAssetResolver assetResolver,
        DiagnosticCollector diagnostics)
    {
        public RenderOptions Options { get; } = options;

        public IAssetResolver AssetResolver { get; } = assetResolver;

        public DiagnosticCollector Diagnostics { get; } = diagnostics;

        public int HeadingCount { get; set; }

        public int TableCount { get; set; }

        public int ImageCount { get; set; }

        public int CodeBlockCount { get; set; }
    }
}
