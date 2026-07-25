using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using DocxTemplater;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DxtNode = DocxTemplater.Schema.TemplateSchemaNode;
using DxtNodeKind = DocxTemplater.Schema.TemplateNodeKind;

namespace Akode.DocxGen.Docx.Inspection;

/// <summary>Inspects placeholders and Markdown style requirements in DOCX packages.</summary>
public sealed partial class DocxTemplateInspector : ITemplateInspector
{
    /// <inheritdoc />
    public TemplateSchema Inspect(
        Stream templateDocument,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateDocument);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = ReadAll(templateDocument);
        var hash =
            $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";
        var diagnostics = new DiagnosticCollector();
        var builders = new Dictionary<string, PlaceholderBuilder>(StringComparer.Ordinal);
        var semanticKinds = new Dictionary<string, ModelValueKind>(
            StringComparer.Ordinal);

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var document = WordprocessingDocument.Open(stream, false);
            if (document.DocumentType == WordprocessingDocumentType.MacroEnabledDocument)
            {
                diagnostics.Add(DiagnosticCode.TemplateMacroEnabled);
            }

            var main = document.MainDocumentPart;
            if (main?.Document is null)
            {
                diagnostics.Add(DiagnosticCode.TemplateNotOoxml);
            }
            else
            {
                Scan(
                    main.Document,
                    "main",
                    builders,
                    semanticKinds,
                    diagnostics);
                foreach (var header in main.HeaderParts)
                {
                    if (header.Header is not null)
                    {
                        Scan(
                            header.Header,
                            "header",
                            builders,
                            semanticKinds,
                            diagnostics);
                    }
                }

                foreach (var footer in main.FooterParts)
                {
                    if (footer.Footer is not null)
                    {
                        Scan(
                            footer.Footer,
                            "footer",
                            builders,
                            semanticKinds,
                            diagnostics);
                    }
                }

                if (main.FootnotesPart?.Footnotes is { } footnotes)
                {
                    Scan(
                        footnotes,
                        "footnotes",
                        builders,
                        semanticKinds,
                        diagnostics);
                }

                if (main.EndnotesPart?.Endnotes is { } endnotes)
                {
                    Scan(
                        endnotes,
                        "endnotes",
                        builders,
                        semanticKinds,
                        diagnostics);
                }

                if (main.WordprocessingCommentsPart?.Comments is { } comments)
                {
                    Scan(
                        comments,
                        "comments",
                        builders,
                        semanticKinds,
                        diagnostics);
                }
            }
        }
        catch (OpenXmlPackageException exception)
        {
            diagnostics.Add(
                DiagnosticCode.TemplateNotOoxml,
                message: $"The template is not a valid DOCX package: {exception.Message}");
        }
        catch (IOException exception)
        {
            diagnostics.Add(
                DiagnosticCode.TemplateNotOoxml,
                message: $"The template could not be read: {exception.Message}");
        }

        var placeholders = builders.Values
            .Select(builder => builder.Build())
            .OrderBy(placeholder => placeholder.Path, StringComparer.Ordinal)
            .ToArray();
        var requiresMarkdown = placeholders.Any(
            placeholder => placeholder.Kind == ModelValueKind.Markdown);
        string[] requiredStyles = requiresMarkdown
            ?
            [
                "Normal",
                "Heading 1",
                "Heading 2",
                "Heading 3",
                "Heading 4",
                "Heading 5",
                "Heading 6",
                "List Paragraph",
                "Quote",
                "Code",
                "CodeInline",
                "Hyperlink",
                "Caption",
                "AkodeTable",
            ]
            : [];
        var roots = diagnostics.HasErrors
            ? []
            : DiscoverShape(bytes, semanticKinds, diagnostics);
        return new TemplateSchema(
            TemplateId: null,
            TemplateVersion: null,
            hash,
            placeholders,
            requiredStyles,
            diagnostics.Items)
        {
            Roots = roots,
        };
    }

    private static byte[] ReadAll(Stream source)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void Scan(
        OpenXmlPartRootElement root,
        string location,
        IDictionary<string, PlaceholderBuilder> builders,
        Dictionary<string, ModelValueKind> semanticKinds,
        DiagnosticCollector diagnostics)
    {
        var stack = new Stack<string>();
        foreach (var paragraph in root.Descendants<Paragraph>())
        {
            var texts = paragraph.Descendants<Text>().ToArray();
            var value = string.Concat(texts.Select(text => text.Text));
            if (value.Length == 0)
            {
                continue;
            }

            var matches = PlaceholderRegex().Matches(value);
            foreach (Match match in matches)
            {
                var token = match.Groups["token"].Value.Trim();
                if (token.StartsWith('#'))
                {
                    var collection = NormalizePath(token[1..], stack);
                    if (!IsModelPath(collection))
                    {
                        continue;
                    }

                    stack.Push(collection);
                    GetOrCreate(builders, collection, ModelValueKind.Collection)
                        .Locations.Add(location);
                    semanticKinds[collection] = ModelValueKind.Collection;
                    continue;
                }

                if (token.StartsWith('/'))
                {
                    var closing = token[1..].Trim();
                    if (stack.Count > 0
                        && closing.Length > 0
                        && (string.Equals(
                                stack.Peek(),
                                closing,
                                StringComparison.Ordinal)
                            || stack.Peek().EndsWith(
                                closing.StartsWith('.')
                                    ? closing
                                    : $".{closing}",
                                StringComparison.Ordinal)))
                    {
                        stack.Pop();
                    }

                    continue;
                }

                if (token.Length == 0
                    || token[0] is '?' or ':' or '@' or '!'
                    || token.StartsWith('('))
                {
                    continue;
                }

                var path = NormalizePath(token, stack);
                if (!IsModelPath(path))
                {
                    diagnostics.Add(
                        DiagnosticCode.TemplateSyntaxError,
                        location,
                        $"Template placeholder '{match.Value}' does not contain a supported model path.");
                    continue;
                }

                var formatter = match.Groups["formatter"].Success
                    ? match.Groups["formatter"].Value
                    : null;
                var kind = formatter?.ToUpperInvariant() switch
                {
                    "MD" => ModelValueKind.Markdown,
                    "IMG" => ModelValueKind.Binary,
                    _ => ModelValueKind.Text,
                };
                if (!semanticKinds.TryGetValue(path, out var existingKind)
                    || existingKind == ModelValueKind.Text
                    || kind == ModelValueKind.Markdown)
                {
                    semanticKinds[path] = kind;
                }

                if (stack.Count > 0)
                {
                    var collectionPath = stack.Peek();
                    var prefix = $"{collectionPath}.";
                    if (path.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        builders[collectionPath].ItemProperties.Add(
                            path[prefix.Length..].Split('.')[0]);
                        continue;
                    }
                }

                var builder = GetOrCreate(builders, path, kind);
                builder.Formatter ??= formatter;
                builder.FormatterArguments ??= match.Groups["args"].Success
                    ? match.Groups["args"].Value
                    : null;
                builder.Locations.Add(location);
                builder.UsedIn ??= paragraph.Ancestors<Table>().Any() ? "table" : "text";

                if (stack.Count > 0)
                {
                    var collection = builders[stack.Peek()];
                    var prefix = $"{stack.Peek()}.";
                    var itemProperty = path.StartsWith(prefix, StringComparison.Ordinal)
                        ? path[prefix.Length..].Split('.')[0]
                        : path.Split('.')[^1];
                    collection.ItemProperties.Add(itemProperty);
                }

                if (IsSplitAcrossTextNodes(match, texts))
                {
                    diagnostics.Add(
                        DiagnosticCode.PlaceholderSplitAcrossRuns,
                        path);
                }
            }
        }
    }

    private static TemplateShapeNode[] DiscoverShape(
        byte[] bytes,
        IReadOnlyDictionary<string, ModelValueKind> semanticKinds,
        DiagnosticCollector diagnostics)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var template = new DocxTemplate(
                stream,
                new ProcessSettings
                {
                    BindingErrorHandling = BindingErrorHandling.ThrowException,
                });
            var schema = template.GetTemplateSchema();
            return schema.Roots.Values
                .OrderBy(node => node.Name, StringComparer.Ordinal)
                .Select(
                    node => ConvertShape(
                        node,
                        node.Name,
                        semanticKinds,
                        collectionItem: false))
                .ToArray();
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or FormatException
                or InvalidDataException
                or InvalidOperationException
                or OpenXmlTemplateException)
        {
            diagnostics.Add(
                DiagnosticCode.TemplateSyntaxError,
                message:
                    $"The template shape could not be analyzed: {exception.Message}",
                hint:
                    "Correct the reported template marker and run inspect again.");
            return [];
        }
    }

    private static TemplateShapeNode ConvertShape(
        DxtNode source,
        string path,
        IReadOnlyDictionary<string, ModelValueKind> semanticKinds,
        bool collectionItem)
    {
        var kind = source.Kind switch
        {
            DxtNodeKind.Collection => ModelValueKind.Collection,
            DxtNodeKind.Object => ModelValueKind.StructuredObject,
            _ => !collectionItem
                && semanticKinds.TryGetValue(path, out var semanticKind)
                ? semanticKind
                : ModelValueKind.Text,
        };
        var properties = source.Properties.Values
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(
                property => ConvertShape(
                    property,
                    $"{path}.{property.Name}",
                    semanticKinds,
                    collectionItem: false))
            .ToArray();
        TemplateShapeNode? item = null;
        if (source.ItemSchema is not null)
        {
            item = ConvertShape(
                source.ItemSchema,
                path,
                semanticKinds,
                collectionItem: true);
        }

        return new TemplateShapeNode(
            source.Name,
            kind,
            properties,
            item);
    }

    private static PlaceholderBuilder GetOrCreate(
        IDictionary<string, PlaceholderBuilder> builders,
        string path,
        ModelValueKind kind)
    {
        if (!builders.TryGetValue(path, out var builder))
        {
            builder = new PlaceholderBuilder(path, kind);
            builders.Add(path, builder);
        }
        else if (kind == ModelValueKind.Markdown)
        {
            builder.Kind = kind;
        }

        return builder;
    }

    private static string NormalizePath(string token, Stack<string> stack)
    {
        var path = token.Trim();
        if (path.Length > 0 && path[0] == '.' && stack.Count > 0)
        {
            return $"{stack.Peek()}.{path.TrimStart('.')}";
        }

        if (!path.Contains('.', StringComparison.Ordinal) && stack.Count > 0)
        {
            return $"{stack.Peek()}.{path}";
        }

        return path;
    }

    private static bool IsModelPath(string path) =>
        path.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .All(segment =>
                segment.Length > 0
                && (char.IsLetter(segment[0]) || segment[0] == '_')
                && segment.All(character =>
                    char.IsLetterOrDigit(character) || character == '_'));

    private static bool IsSplitAcrossTextNodes(
        Match match,
        IReadOnlyList<Text> texts)
    {
        var start = 0;
        var matches = 0;
        foreach (var text in texts)
        {
            var end = start + text.Text.Length;
            if (text.Text.Length > 0
                && match.Index < end
                && match.Index + match.Length > start)
            {
                matches++;
            }

            start = end;
        }

        return matches > 1;
    }

    [GeneratedRegex(
        @"\{\{\s*(?<token>[^{}]+?)\s*\}(?:\}|:(?<formatter>[A-Za-z][A-Za-z0-9_-]*)(?:\((?<args>[^)]*)\))?\})",
        RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    private sealed class PlaceholderBuilder(string path, ModelValueKind kind)
    {
        public string Path { get; } = path;

        public ModelValueKind Kind { get; set; } = kind;

        public string? Formatter { get; set; }

        public string? FormatterArguments { get; set; }

        public HashSet<string> Locations { get; } = new(StringComparer.Ordinal);

        public HashSet<string> ItemProperties { get; } = new(StringComparer.Ordinal);

        public string? UsedIn { get; set; }

        public TemplatePlaceholder Build() =>
            new(
                Path,
                Kind,
                Formatter,
                FormatterArguments,
                Required: true,
                Locations.Order(StringComparer.Ordinal).ToArray(),
                ItemProperties.Order(StringComparer.Ordinal).ToArray(),
                UsedIn);
    }
}
