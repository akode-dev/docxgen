using System.Globalization;
using System.Text;
using System.Xml;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Core.Security;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace Akode.DocxGen.Docx.Extraction;

/// <summary>Extracts a portable semantic Markdown representation from DOCX.</summary>
public sealed class DocxMarkdownExtractor : IDocxMarkdownExtractor
{
    /// <inheritdoc />
    public async Task<ExtractResult> ExtractAsync(
        ExtractRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        byte[] bytes;
        try
        {
            bytes = await ReadAllAsync(
                request.Document.Content,
                Limits.Default.MaxDocumentBytes,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException exception)
        {
            return Failure(request.Document.Name, exception.Message);
        }

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var package = WordprocessingDocument.Open(
                stream,
                false,
                new OpenSettings
                {
                    AutoSave = false,
                    MaxCharactersInPart =
                        Limits.Default.MaxDocumentPartCharacters,
                });
            if (package.GetAllParts()
                .Take(Limits.Default.MaxDocumentParts + 1)
                .Count() > Limits.Default.MaxDocumentParts)
            {
                return Failure(
                    request.Document.Name,
                    $"The DOCX contains more than "
                    + $"{Limits.Default.MaxDocumentParts} related parts.");
            }

            if (package.DocumentType is WordprocessingDocumentType.MacroEnabledDocument
                or WordprocessingDocumentType.MacroEnabledTemplate)
            {
                return Failure(
                    request.Document.Name,
                    "Macro-enabled Word documents are not accepted for extraction.");
            }

            var main = package.MainDocumentPart;
            var body = main?.Document?.Body;
            if (main is null || body is null)
            {
                return Failure(
                    request.Document.Name,
                    "The DOCX has no readable main document body.");
            }

            var context = new ExtractionContext(
                main,
                request.ImagePathPrefix,
                cancellationToken);
            var markdown = context.Render(body);
            return new ExtractResult(
                markdown,
                context.Assets,
                context.Stats,
                context.Diagnostics);
        }
        catch (Exception exception) when (
            exception is OpenXmlPackageException
                or FileFormatException
                or InvalidDataException
                or XmlException)
        {
            return Failure(request.Document.Name, exception.Message);
        }
    }

    private static ExtractResult Failure(string path, string message) =>
        new(
            string.Empty,
            [],
            DocxExtractionStats.Empty,
            [
                DiagnosticRegistry.Create(
                    DiagnosticCode.ExtractionFailure,
                    path,
                    message),
            ]);

    private static async Task<byte[]> ReadAllAsync(
        Stream source,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        while (true)
        {
            var read = await source.ReadAsync(
                chunk,
                cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > maxBytes)
            {
                throw new InvalidDataException(
                    $"The DOCX exceeds the {maxBytes}-byte extraction limit.");
            }

            await buffer.WriteAsync(
                chunk.AsMemory(0, read),
                cancellationToken).ConfigureAwait(false);
        }

        return buffer.ToArray();
    }

    private sealed class ExtractionContext
    {
        private readonly MainDocumentPart main;
        private readonly string imagePathPrefix;
        private readonly CancellationToken cancellationToken;
        private readonly DiagnosticCollector diagnostics = new();
        private readonly List<ExtractedAsset> assets = [];
        private readonly Dictionary<string, ExtractedAsset> assetsByPart =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> warnings = new(StringComparer.Ordinal);
        private readonly StyleResolver styles;
        private readonly NumberingResolver numbering;
        private int paragraphCount;
        private int headingCount;
        private int listItemCount;
        private int tableCount;
        private int imageCount;
        private long extractedAssetBytes;

        public ExtractionContext(
            MainDocumentPart main,
            string imagePathPrefix,
            CancellationToken cancellationToken)
        {
            this.main = main;
            this.imagePathPrefix = imagePathPrefix;
            this.cancellationToken = cancellationToken;
            styles = new StyleResolver(
                main.StyleDefinitionsPart?.Styles,
                WarnDowngrade);
            numbering = new NumberingResolver(
                main.NumberingDefinitionsPart?.Numbering,
                WarnDowngrade);
        }

        public IReadOnlyList<ExtractedAsset> Assets => assets.AsReadOnly();

        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics.Items;

        public DocxExtractionStats Stats =>
            new(paragraphCount, headingCount, listItemCount, tableCount, imageCount);

        public string Render(Body body)
        {
            var blocks = new List<string>();
            var list = new StringBuilder();
            foreach (var element in body.ChildElements)
            {
                RenderBlock(element, blocks, list);
            }

            FlushList(blocks, list);
            return blocks.Count == 0
                ? string.Empty
                : string.Join("\n\n", blocks) + "\n";
        }

        private void RenderBlock(
            OpenXmlElement element,
            List<string> blocks,
            StringBuilder list)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (element)
            {
                case Paragraph paragraph:
                    {
                        paragraphCount++;
                        var rendered = RenderParagraph(paragraph, blockContext: true);
                        if (rendered.IsListItem)
                        {
                            if (rendered.Text.Length > 0)
                            {
                                if (list.Length > 0)
                                {
                                    list.Append('\n');
                                }

                                list.Append(rendered.Text);
                            }

                            return;
                        }

                        FlushList(blocks, list);
                        if (rendered.Text.Length > 0)
                        {
                            blocks.Add(rendered.Text);
                        }

                        break;
                    }

                case Table table:
                    FlushList(blocks, list);
                    var renderedTable = RenderTable(table);
                    if (renderedTable.Length > 0)
                    {
                        blocks.Add(renderedTable);
                    }

                    tableCount++;
                    break;
                case SdtBlock structured:
                    foreach (var child in structured.SdtContentBlock?.ChildElements ?? [])
                    {
                        RenderBlock(child, blocks, list);
                    }

                    break;
                case SectionProperties:
                    break;
                default:
                    FlushList(blocks, list);
                    WarnDowngrade(
                        "block:" + element.LocalName,
                        $"/word/document.xml/{element.LocalName}",
                        $"The Word block '{element.LocalName}' was omitted.");
                    break;
            }
        }

        private RenderedParagraph RenderParagraph(
            Paragraph paragraph,
            bool blockContext)
        {
            var properties = paragraph.ParagraphProperties;
            if (HasHorizontalRule(properties)
                && string.IsNullOrWhiteSpace(paragraph.InnerText))
            {
                return new RenderedParagraph("---", false);
            }

            var inline = RenderInlineChildren(paragraph.ChildElements);
            if (string.IsNullOrWhiteSpace(inline))
            {
                if (paragraph.Descendants<Break>().Any(
                        item => item.Type?.Value == BreakValues.Page))
                {
                    WarnDowngrade(
                        "page-break",
                        "/word/document.xml",
                        "A Word page break was omitted because Markdown has no portable page-break semantic.");
                }

                return RenderedParagraph.Empty;
            }

            var style = styles.ResolveParagraph(properties);
            var headingLevel = styles.HeadingLevel(properties);
            if (headingLevel >= 1 && blockContext)
            {
                var markdownLevel = Math.Min(headingLevel, 6);
                if (headingLevel > markdownLevel)
                {
                    WarnDowngrade(
                        $"heading-level:{headingLevel}",
                        "/word/document.xml",
                        $"Word heading level {headingLevel} was represented as Markdown heading level 6.");
                }

                headingCount++;
                return new RenderedParagraph(
                    new string('#', markdownLevel) + " " + inline.Trim(),
                    false);
            }

            var listInfo = numbering.Resolve(properties);
            if (listInfo is not null && blockContext)
            {
                listItemCount++;
                var indentation = new string(' ', listInfo.Level * 4);
                var marker = listInfo.Ordered
                    ? listInfo.Number.ToString(CultureInfo.InvariantCulture) + "."
                    : "-";
                return new RenderedParagraph(
                    $"{indentation}{marker} {inline.Trim()}",
                    true);
            }

            if (style is "Code" && blockContext)
            {
                var code = UnescapeMarkdown(inline.TrimEnd());
                var fence = new string('`', Math.Max(3, LongestRun(code, '`') + 1));
                return new RenderedParagraph(
                    $"{fence}\n{code}\n{fence}",
                    false);
            }

            if (style is "Quote" && blockContext)
            {
                return new RenderedParagraph(
                    string.Join(
                        "\n",
                        inline.Trim().Split('\n').Select(line => $"> {line}")),
                    false);
            }

            if (style is "Caption" && blockContext)
            {
                return new RenderedParagraph($"*{inline.Trim()}*", false);
            }

            return new RenderedParagraph(inline.Trim(), false);
        }

        private string RenderInlineChildren(IEnumerable<OpenXmlElement> elements)
        {
            var fieldState = new FieldState();
            var result = RenderInlineChildren(elements, fieldState);
            if (fieldState.Depth > 0)
            {
                WarnFieldOmitted(fieldState);
            }

            return result;
        }

        private string RenderInlineChildren(
            IEnumerable<OpenXmlElement> elements,
            FieldState fieldState)
        {
            var builder = new StringBuilder();
            foreach (var element in elements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (element)
                {
                    case ParagraphProperties:
                    case BookmarkStart:
                    case BookmarkEnd:
                    case ProofError:
                    case PermStart:
                    case PermEnd:
                        break;
                    case DeletedRun:
                        break;
                    case Run run:
                        builder.Append(RenderRun(run, fieldState));
                        break;
                    case Hyperlink hyperlink:
                        builder.Append(RenderHyperlink(hyperlink, fieldState));
                        break;
                    case SimpleField:
                        WarnOnce(
                            DiagnosticCode.ExtractionFieldOmitted,
                            "simple-field",
                            "/word/document.xml",
                            "A simple Word field was omitted.");
                        break;
                    default:
                        if (element.ChildElements.Count > 0)
                        {
                            builder.Append(
                                RenderInlineChildren(
                                    element.ChildElements,
                                    fieldState));
                        }
                        else
                        {
                            WarnDowngrade(
                                "inline:" + element.LocalName,
                                "/word/document.xml",
                                $"The inline Word element '{element.LocalName}' was omitted.");
                        }

                        break;
                }
            }

            return builder.ToString();
        }

        private string RenderRun(Run run, FieldState fieldState)
        {
            var properties = run.RunProperties;
            if (IsOn(properties?.Vanish))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var child in run.ChildElements)
            {
                if (child is FieldChar fieldCharacter)
                {
                    var type = fieldCharacter.FieldCharType?.Value;
                    if (type == FieldCharValues.Begin)
                    {
                        fieldState.Depth++;
                        fieldState.Code.Clear();
                    }
                    else if (type == FieldCharValues.End)
                    {
                        WarnFieldOmitted(fieldState);
                        fieldState.Depth = Math.Max(0, fieldState.Depth - 1);
                        if (fieldState.Depth == 0)
                        {
                            fieldState.Code.Clear();
                        }
                    }

                    continue;
                }

                if (child is FieldCode fieldCode)
                {
                    fieldState.Code.Append(fieldCode.Text);
                    continue;
                }

                if (fieldState.Depth > 0)
                {
                    continue;
                }

                switch (child)
                {
                    case RunProperties:
                        break;
                    case Text text:
                        builder.Append(EscapeMarkdown(text.Text));
                        break;
                    case TabChar:
                        builder.Append('\t');
                        break;
                    case Break lineBreak when lineBreak.Type?.Value != BreakValues.Page:
                    case CarriageReturn:
                        builder.Append("  \n");
                        break;
                    case Break:
                        WarnDowngrade(
                            "page-break",
                            "/word/document.xml",
                            "A Word page break was omitted because Markdown has no portable page-break semantic.");
                        break;
                    case NoBreakHyphen:
                        builder.Append('-');
                        break;
                    case SoftHyphen:
                        break;
                    case Drawing drawing:
                        builder.Append(RenderImage(drawing));
                        break;
                    default:
                        if (child.ChildElements.Count > 0)
                        {
                            builder.Append(
                                RenderInlineChildren(
                                    child.ChildElements,
                                    fieldState));
                        }
                        else
                        {
                            WarnDowngrade(
                                "run:" + child.LocalName,
                                "/word/document.xml",
                                $"The run element '{child.LocalName}' was omitted.");
                        }

                        break;
                }
            }

            var content = builder.ToString();
            if (content.Length == 0)
            {
                return content;
            }

            var runStyle = styles.ResolveRun(properties);
            var code = runStyle is "CodeInline";
            var bold = IsOn(properties?.Bold) || styles.IsBold(runStyle);
            var italic = IsOn(properties?.Italic) || styles.IsItalic(runStyle);
            var strike = IsOn(properties?.Strike);

            if (code)
            {
                var raw = UnescapeMarkdown(content);
                var delimiter = new string(
                    '`',
                    Math.Max(1, LongestRun(raw, '`') + 1));
                content = $"{delimiter}{raw}{delimiter}";
            }

            if (bold)
            {
                content = $"**{content}**";
            }

            if (italic)
            {
                content = $"*{content}*";
            }

            if (strike)
            {
                content = $"~~{content}~~";
            }

            return content;
        }

        private string RenderHyperlink(
            Hyperlink hyperlink,
            FieldState fieldState)
        {
            var label = RenderInlineChildren(
                hyperlink.ChildElements,
                fieldState);
            if (label.Length == 0)
            {
                return string.Empty;
            }

            string? target = null;
            var relationshipId = hyperlink.Id?.Value;
            if (!string.IsNullOrWhiteSpace(relationshipId))
            {
                target = main.HyperlinkRelationships
                    .FirstOrDefault(item => item.Id == relationshipId)?
                    .Uri
                    .OriginalString;
            }

            if (target is null && !string.IsNullOrWhiteSpace(hyperlink.Anchor?.Value))
            {
                target = "#" + hyperlink.Anchor.Value;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                WarnDowngrade(
                    "hyperlink-target",
                    "/word/document.xml",
                    "A hyperlink without a resolvable target was emitted as plain text.");
                return label;
            }

            return $"[{label}]({EscapeLinkTarget(target)})";
        }

        private void WarnFieldOmitted(FieldState fieldState)
        {
            var code = fieldState.Code.ToString().Trim();
            WarnOnce(
                DiagnosticCode.ExtractionFieldOmitted,
                code.Length == 0 ? "complex-field" : "field:" + code,
                "/word/document.xml",
                code.Length == 0
                    ? "A complex Word field was omitted."
                    : $"The Word field '{code}' was omitted.");
        }

        private string RenderImage(Drawing drawing)
        {
            var relationshipId = drawing
                .Descendants<A.Blip>()
                .Select(blip => blip.Embed?.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (relationshipId is null
                || !TryGetImagePart(relationshipId, out var imagePart))
            {
                WarnDowngrade(
                    "image-relationship",
                    "/word/document.xml",
                    "An image with no readable embedded relationship was omitted.");
                return string.Empty;
            }

            var partKey = imagePart.Uri.OriginalString;
            if (!assetsByPart.TryGetValue(partKey, out var asset))
            {
                if (assets.Count >= Limits.Default.MaxExtractedAssets)
                {
                    throw new InvalidDataException(
                        $"The DOCX contains more than "
                        + $"{Limits.Default.MaxExtractedAssets} extractable assets.");
                }

                using var imageStream = imagePart.GetStream(
                    FileMode.Open,
                    FileAccess.Read);
                using var buffer = new MemoryStream();
                imageStream.CopyTo(buffer);
                if (buffer.Length > Limits.Default.MaxAssetBytes
                    || extractedAssetBytes + buffer.Length
                    > Limits.Default.MaxExtractedAssetBytes)
                {
                    throw new InvalidDataException(
                        "Extracted image data exceeds the configured resource limits.");
                }

                var number = assets.Count + 1;
                asset = new ExtractedAsset(
                    $"image-{number:000}{ExtensionFor(imagePart.ContentType)}",
                    imagePart.ContentType,
                    buffer.ToArray());
                assets.Add(asset);
                assetsByPart.Add(partKey, asset);
                extractedAssetBytes += asset.Content.Length;
            }

            imageCount++;
            var properties = drawing.Descendants<DW.DocProperties>().FirstOrDefault();
            var alt = properties?.Description?.Value;
            if (string.IsNullOrWhiteSpace(alt))
            {
                alt = properties?.Name?.Value;
            }

            alt = string.IsNullOrWhiteSpace(alt) ? "image" : alt.Trim();
            return $"![{EscapeAltText(alt)}]({ImagePath(asset.FileName)})";
        }

        private bool TryGetImagePart(
            string relationshipId,
            out ImagePart imagePart)
        {
            try
            {
                if (main.GetPartById(relationshipId) is ImagePart found)
                {
                    imagePart = found;
                    return true;
                }

                imagePart = null!;
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                imagePart = null!;
                return false;
            }
            catch (KeyNotFoundException)
            {
                imagePart = null!;
                return false;
            }
        }

        private string RenderTable(Table table)
        {
            var rows = new List<List<string>>();
            var hasExplicitHeader = false;
            foreach (var row in table.Elements<TableRow>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (rows.Count == 0)
                {
                    hasExplicitHeader =
                        row.TableRowProperties?.GetFirstChild<TableHeader>() is not null
                        || LooksLikeHeader(row);
                }

                var cells = new List<string>();
                foreach (var cell in row.Elements<TableCell>())
                {
                    if (cell.TableCellProperties?.GridSpan?.Val?.Value > 1
                        || cell.TableCellProperties?.VerticalMerge is not null)
                    {
                        WarnDowngrade(
                            "merged-table-cell",
                            "/word/document.xml",
                            "Merged Word table cells were flattened in Markdown.");
                    }

                    var paragraphs = new List<string>();
                    foreach (var paragraph in cell.Elements<Paragraph>())
                    {
                        paragraphCount++;
                        var value = RenderParagraph(paragraph, blockContext: false).Text;
                        if (value.Length > 0)
                        {
                            paragraphs.Add(value);
                        }
                    }

                    cells.Add(EscapeTableCell(string.Join("<br>", paragraphs)));
                }

                rows.Add(cells);
            }

            if (rows.Count == 0)
            {
                WarnDowngrade(
                    "empty-table",
                    "/word/document.xml",
                    "An empty Word table was omitted.");
                return string.Empty;
            }

            if (!hasExplicitHeader)
            {
                WarnOnce(
                    DiagnosticCode.ExtractionTableHeaderInferred,
                    "table-header",
                    "/word/document.xml");
            }

            var width = rows.Max(row => row.Count);
            foreach (var row in rows)
            {
                while (row.Count < width)
                {
                    row.Add(string.Empty);
                }
            }

            var builder = new StringBuilder();
            AppendTableRow(builder, rows[0]);
            AppendTableRow(builder, Enumerable.Repeat("---", width));
            foreach (var row in rows.Skip(1))
            {
                AppendTableRow(builder, row);
            }

            return builder.ToString().TrimEnd();
        }

        private static bool LooksLikeHeader(TableRow row)
        {
            var textRuns = row
                .Descendants<Run>()
                .Where(run => run.Descendants<Text>().Any())
                .ToArray();
            return textRuns.Length > 0
                && textRuns.All(run => IsOn(run.RunProperties?.Bold));
        }

        private string ImagePath(string fileName) =>
            imagePathPrefix is "."
                ? fileName
                : $"{imagePathPrefix}/{fileName}";

        private void WarnDowngrade(string key, string path, string message) =>
            WarnOnce(
                DiagnosticCode.ExtractionFeatureDowngraded,
                key,
                path,
                message);

        private void WarnOnce(
            string code,
            string key,
            string path,
            string? message = null)
        {
            if (warnings.Add(code + ":" + key))
            {
                diagnostics.Add(code, path, message);
            }
        }

        private static bool HasHorizontalRule(ParagraphProperties? properties) =>
            properties?.ParagraphBorders?.BottomBorder is not null;

        private static void FlushList(List<string> blocks, StringBuilder list)
        {
            if (list.Length == 0)
            {
                return;
            }

            blocks.Add(list.ToString());
            list.Clear();
        }

        private static void AppendTableRow(
            StringBuilder builder,
            IEnumerable<string> cells)
        {
            builder.Append("| ");
            builder.Append(string.Join(" | ", cells));
            builder.AppendLine(" |");
        }

        private static string EscapeMarkdown(string value) =>
            value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("*", "\\*", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal)
                .Replace("[", "\\[", StringComparison.Ordinal)
                .Replace("]", "\\]", StringComparison.Ordinal);

        private static string UnescapeMarkdown(string value) =>
            value
                .Replace("\\]", "]", StringComparison.Ordinal)
                .Replace("\\[", "[", StringComparison.Ordinal)
                .Replace("\\_", "_", StringComparison.Ordinal)
                .Replace("\\*", "*", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal);

        private static string EscapeAltText(string value) =>
            value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("[", "\\[", StringComparison.Ordinal)
                .Replace("]", "\\]", StringComparison.Ordinal);

        private static string EscapeLinkTarget(string value) =>
            value
                .Replace(" ", "%20", StringComparison.Ordinal)
                .Replace("(", "%28", StringComparison.Ordinal)
                .Replace(")", "%29", StringComparison.Ordinal);

        private static string EscapeTableCell(string value) =>
            value
                .Replace("|", "\\|", StringComparison.Ordinal)
                .Replace("\r\n", "<br>", StringComparison.Ordinal)
                .Replace("\n", "<br>", StringComparison.Ordinal);

        private static string ExtensionFor(string contentType) =>
            contentType.ToUpperInvariant() switch
            {
                "IMAGE/PNG" => ".png",
                "IMAGE/JPEG" => ".jpg",
                "IMAGE/GIF" => ".gif",
                "IMAGE/BMP" => ".bmp",
                "IMAGE/SVG+XML" => ".svg",
                "IMAGE/TIFF" => ".tiff",
                "IMAGE/X-EMF" => ".emf",
                "IMAGE/X-WMF" => ".wmf",
                "IMAGE/WEBP" => ".webp",
                _ => ".bin",
            };

        private static int LongestRun(string value, char target)
        {
            var longest = 0;
            var current = 0;
            foreach (var character in value)
            {
                if (character == target)
                {
                    current++;
                    longest = Math.Max(longest, current);
                }
                else
                {
                    current = 0;
                }
            }

            return longest;
        }

        private static bool IsOn(OnOffType? property) =>
            property is not null && property.Val?.Value != false;

        private sealed record RenderedParagraph(string Text, bool IsListItem)
        {
            public static RenderedParagraph Empty { get; } = new(string.Empty, false);
        }

        private sealed class FieldState
        {
            public int Depth { get; set; }

            public StringBuilder Code { get; } = new();
        }
    }

    private sealed class StyleResolver
    {
        private readonly Dictionary<string, Style> byId;

        public StyleResolver(
            Styles? styles,
            Action<string, string, string> warn)
        {
            byId = new Dictionary<string, Style>(StringComparer.OrdinalIgnoreCase);
            foreach (var style in styles?.Elements<Style>() ?? [])
            {
                var styleId = style.StyleId?.Value;
                if (string.IsNullOrWhiteSpace(styleId))
                {
                    continue;
                }

                if (!byId.TryAdd(styleId, style))
                {
                    warn(
                        $"duplicate-style:{styleId}",
                        "/word/styles.xml",
                        $"Duplicate Word style identifier '{styleId}' was ignored; the first definition was used.");
                }
            }
        }

        public string? ResolveParagraph(ParagraphProperties? properties) =>
            CanonicalName(properties?.ParagraphStyleId?.Val?.Value);

        public string? ResolveRun(RunProperties? properties) =>
            CanonicalName(properties?.RunStyle?.Val?.Value);

        public int HeadingLevel(ParagraphProperties? properties)
        {
            var direct = properties?.OutlineLevel?.Val?.Value;
            if (direct is >= 0 and <= 8)
            {
                return direct.Value + 1;
            }

            var styleId = properties?.ParagraphStyleId?.Val?.Value;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!string.IsNullOrWhiteSpace(styleId) && visited.Add(styleId))
            {
                if (HeadingFromName(styleId) is { } byIdLevel)
                {
                    return byIdLevel;
                }

                if (!byId.TryGetValue(styleId, out var style))
                {
                    break;
                }

                if (HeadingFromName(style.StyleName?.Val?.Value) is { } byNameLevel)
                {
                    return byNameLevel;
                }

                var outline = style.StyleParagraphProperties?
                    .OutlineLevel?
                    .Val?
                    .Value;
                if (outline is >= 0 and <= 8)
                {
                    return outline.Value + 1;
                }

                styleId = style.BasedOn?.Val?.Value;
            }

            return 0;
        }

        public bool IsBold(string? styleName) =>
            StyleProperty(styleName, properties => properties.GetFirstChild<Bold>());

        public bool IsItalic(string? styleName) =>
            StyleProperty(styleName, properties => properties.GetFirstChild<Italic>());

        private bool StyleProperty(
            string? styleName,
            Func<StyleRunProperties, OnOffType?> selector)
        {
            if (styleName is null)
            {
                return false;
            }

            var style = byId.Values.FirstOrDefault(
                item => string.Equals(
                    CanonicalName(item.StyleId?.Value),
                    styleName,
                    StringComparison.OrdinalIgnoreCase));
            var property = style?.StyleRunProperties is { } properties
                ? selector(properties)
                : null;
            return property is not null && property.Val?.Value != false;
        }

        private string? CanonicalName(string? styleId)
        {
            if (string.IsNullOrWhiteSpace(styleId))
            {
                return null;
            }

            var name = byId.TryGetValue(styleId, out var style)
                ? style.StyleName?.Val?.Value ?? styleId
                : styleId;
            var compact = name.Replace(" ", string.Empty, StringComparison.Ordinal);
            if (compact.Equals("CodeInline", StringComparison.OrdinalIgnoreCase))
            {
                return "CodeInline";
            }

            if (compact.Equals("Code", StringComparison.OrdinalIgnoreCase))
            {
                return "Code";
            }

            if (compact.Equals("Quote", StringComparison.OrdinalIgnoreCase)
                || compact.Equals("IntenseQuote", StringComparison.OrdinalIgnoreCase))
            {
                return "Quote";
            }

            if (compact.Equals("Caption", StringComparison.OrdinalIgnoreCase))
            {
                return "Caption";
            }

            return styleId;
        }

        private static int? HeadingFromName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var compact = new string(
                value.Where(character =>
                    !char.IsWhiteSpace(character)
                    && character is not '-' and not '_')
                .ToArray());
            if (!compact.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(
                compact["Heading".Length..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var level)
                && level >= 1
                    ? level
                    : null;
        }
    }

    private sealed class NumberingResolver
    {
        private readonly Dictionary<int, NumberingInstance> instances;
        private readonly Dictionary<int, AbstractNum> abstracts;
        private readonly Dictionary<(int NumberingId, int Level), int> next = [];
        private readonly Action<string, string, string> warn;

        public NumberingResolver(
            Numbering? numbering,
            Action<string, string, string> warn)
        {
            this.warn = warn;
            instances = [];
            foreach (var instance in numbering?.Elements<NumberingInstance>() ?? [])
            {
                var numberId = instance.NumberID?.Value;
                if (numberId is null)
                {
                    continue;
                }

                if (!instances.TryAdd(numberId.Value, instance))
                {
                    warn(
                        $"duplicate-numbering:{numberId.Value}",
                        "/word/numbering.xml",
                        $"Duplicate Word numbering identifier '{numberId.Value}' was ignored; the first definition was used.");
                }
            }

            abstracts = [];
            foreach (var abstractNumber in numbering?.Elements<AbstractNum>() ?? [])
            {
                var abstractId = abstractNumber.AbstractNumberId?.Value;
                if (abstractId is null)
                {
                    continue;
                }

                if (!abstracts.TryAdd(abstractId.Value, abstractNumber))
                {
                    warn(
                        $"duplicate-abstract-numbering:{abstractId.Value}",
                        "/word/numbering.xml",
                        $"Duplicate Word abstract numbering identifier '{abstractId.Value}' was ignored; the first definition was used.");
                }
            }
        }

        public ListInfo? Resolve(ParagraphProperties? properties)
        {
            var numberingProperties = properties?.NumberingProperties;
            var numberingId = numberingProperties?.NumberingId?.Val?.Value;
            if (numberingId is null)
            {
                return null;
            }

            var level = Math.Max(
                0,
                numberingProperties?.NumberingLevelReference?.Val?.Value ?? 0);
            var ordered = true;
            var start = 1;
            if (instances.TryGetValue(numberingId.Value, out var instance)
                && instance.AbstractNumId?.Val?.Value is { } abstractId
                && abstracts.TryGetValue(abstractId, out var abstractNumbering))
            {
                var definition = abstractNumbering
                    .Elements<Level>()
                    .FirstOrDefault(item => item.LevelIndex?.Value == level);
                ordered = definition?.NumberingFormat?.Val?.Value
                    != NumberFormatValues.Bullet;
                start = definition?.StartNumberingValue?.Val?.Value ?? 1;
                var overrideValue = instance
                    .Elements<LevelOverride>()
                    .FirstOrDefault(item => item.LevelIndex?.Value == level)?
                    .StartOverrideNumberingValue?
                    .Val?
                    .Value;
                if (overrideValue is not null)
                {
                    start = overrideValue.Value;
                }

                var format = definition?.NumberingFormat?.Val?.Value;
                if (ordered && format is not null && format != NumberFormatValues.Decimal)
                {
                    warn(
                        "number-format:" + format,
                        "/word/numbering.xml",
                        $"The Word list format '{format}' was represented with decimal Markdown markers.");
                }
            }

            var key = (numberingId.Value, level);
            var number = next.TryGetValue(key, out var current) ? current : start;
            next[key] = number + 1;
            return new ListInfo(ordered, level, number);
        }

        public sealed record ListInfo(bool Ordered, int Level, int Number);
    }
}
