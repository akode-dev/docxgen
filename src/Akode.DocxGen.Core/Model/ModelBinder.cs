using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Core.Security;

namespace Akode.DocxGen.Core.Model;

/// <summary>Resolves model directives and produces values consumable by adapters.</summary>
public static class ModelBinder
{
    /// <summary>Binds one merged model tree using a local-asset policy.</summary>
    public static ModelBindingResult Bind(
        IReadOnlyDictionary<string, ModelValue> data,
        TemplateSchema schema,
        RenderOptions options,
        IAssetResolver assetResolver)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(assetResolver);

        var context = new BindingContext(options, assetResolver);
        var roots = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var item in data)
        {
            roots[item.Key] = BindValue(item.Value, item.Key, depth: 1, context);
        }

        var unbound = new List<string>();
        var collectionPaths = schema.Placeholders
            .Where(placeholder => placeholder.Kind == ModelValueKind.Collection)
            .Select(placeholder => placeholder.Path)
            .ToArray();
        foreach (var placeholder in schema.Placeholders)
        {
            if (collectionPaths.Any(
                    collection =>
                        placeholder.Path.Length > collection.Length
                        && placeholder.Path.StartsWith(
                            collection + ".",
                            StringComparison.Ordinal)))
            {
                continue;
            }

            if (!TryResolve(roots, placeholder.Path, out var value) || IsMissing(value))
            {
                unbound.Add(placeholder.Path);
                if (placeholder.Required || options.Strict)
                {
                    context.Diagnostics.Add(
                        DiagnosticCode.ModelUnboundPlaceholders,
                        ToPointer(placeholder.Path),
                        $"Template placeholder '{placeholder.Path}' is not bound.",
                        $"Set data.{placeholder.Path} to a non-empty value.");
                }
                else
                {
                    context.Diagnostics.Add(
                        DiagnosticCode.OptionalPlaceholderUnbound,
                        ToPointer(placeholder.Path),
                        $"Optional template placeholder '{placeholder.Path}' is not bound.");
                }
            }
        }

        var stats = new MarkdownStats(
            context.MarkdownSections,
            context.MarkdownHeadings,
            context.MarkdownTables,
            context.MarkdownImages,
            context.MarkdownCodeBlocks);
        var model = context.Diagnostics.HasErrors
            ? null
            : new BoundModel(
                new ReadOnlyDictionary<string, object?>(roots),
                context.BoundPaths.Order(StringComparer.Ordinal).ToArray(),
                unbound.Order(StringComparer.Ordinal).ToArray(),
                stats)
            {
                DocumentVersion = FindDocumentVersion(roots),
            };
        return new ModelBindingResult(model, context.Diagnostics.Items);
    }

    private static object? BindValue(
        ModelValue value,
        string path,
        int depth,
        BindingContext context)
    {
        context.ValueCount++;
        if (depth > context.Options.Limits.MaxModelDepth
            || context.ValueCount > context.Options.Limits.MaxModelValues)
        {
            context.Diagnostics.Add(
                DiagnosticCode.ModelSchemaViolation,
                ToPointer(path),
                "The model exceeds configured recursion or value-count limits.",
                "Reduce model nesting or collection sizes.");
            return null;
        }

        switch (value)
        {
            case PrimitiveModelValue primitive:
                context.BoundPaths.Add(path);
                return PrimitiveValue(primitive.Value);

            case PlainTextModelValue text:
                context.BoundPaths.Add(path);
                return text.Text;

            case InlineMarkdownModelValue markdown:
                context.BoundPaths.Add(path);
                return ParseMarkdown(markdown.Markdown, context);

            case MarkdownFileModelValue markdownFile:
                context.BoundPaths.Add(path);
                try
                {
                    var asset = context.AssetResolver.Resolve(
                        markdownFile.RelativePath,
                        AssetKind.Markdown);
                    return ParseMarkdown(
                        Encoding.UTF8.GetString(asset.Content.Span),
                        context);
                }
                catch (AssetResolutionException exception)
                {
                    context.Diagnostics.Add(exception.Diagnostic);
                    return null;
                }

            case FileModelValue file:
                context.BoundPaths.Add(path);
                try
                {
                    return context.AssetResolver.Resolve(
                        file.RelativePath,
                        AssetKind.Binary);
                }
                catch (AssetResolutionException exception)
                {
                    context.Diagnostics.Add(exception.Diagnostic);
                    return null;
                }

            case CollectionModelValue collection:
                context.BoundPaths.Add(path);
                return collection.Items
                    .Select((item, index) => BindValue(
                        item,
                        $"{path}.{index}",
                        depth + 1,
                        context))
                    .ToArray();

            case ObjectModelValue structured:
                var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in structured.Properties)
                {
                    properties[property.Key] = BindValue(
                        property.Value,
                        $"{path}.{property.Key}",
                        depth + 1,
                        context);
                }

                return properties;

            default:
                throw new InvalidOperationException(
                    $"Unsupported model value type '{value.GetType().FullName}'.");
        }
    }

    private static MarkdownContent ParseMarkdown(
        string markdown,
        BindingContext context)
    {
        var content = MarkdownContentParser.Parse(
            markdown,
            context.Options,
            context.AssetResolver);
        foreach (var diagnostic in content.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic);
        }

        context.MarkdownSections += content.Stats.Sections;
        context.MarkdownHeadings += content.Stats.Headings;
        context.MarkdownTables += content.Stats.Tables;
        context.MarkdownImages += content.Stats.Images;
        context.MarkdownCodeBlocks += content.Stats.CodeBlocks;
        return content;
    }

    private static object? PrimitiveValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new InvalidOperationException(
                $"Unsupported primitive JSON kind '{value.ValueKind}'."),
        };

    private static bool TryResolve(
        IReadOnlyDictionary<string, object?> roots,
        string path,
        out object? value)
    {
        value = roots;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (value is not IReadOnlyDictionary<string, object?> dictionary
                || !dictionary.TryGetValue(segment, out value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMissing(object? value) =>
        value is null
        || value is string text && string.IsNullOrWhiteSpace(text);

    private static string? FindDocumentVersion(
        IReadOnlyDictionary<string, object?> roots) =>
        TryResolve(roots, "ds.Document.Version", out var value)
            ? value?.ToString()
            : null;

    private static string ToPointer(string path) =>
        $"/data/{path.Replace(".", "/", StringComparison.Ordinal)}";

    private sealed class BindingContext(
        RenderOptions options,
        IAssetResolver assetResolver)
    {
        public RenderOptions Options { get; } = options;

        public IAssetResolver AssetResolver { get; } = assetResolver;

        public DiagnosticCollector Diagnostics { get; } = new();

        public HashSet<string> BoundPaths { get; } = new(StringComparer.Ordinal);

        public int ValueCount { get; set; }

        public int MarkdownSections { get; set; }

        public int MarkdownHeadings { get; set; }

        public int MarkdownTables { get; set; }

        public int MarkdownImages { get; set; }

        public int MarkdownCodeBlocks { get; set; }
    }
}
