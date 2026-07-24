using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;

namespace Akode.DocxGen.Core.Model;

/// <summary>
/// Reconciles anchored Markdown, JSON model data, and command-line overrides
/// with deterministic <c>override &gt; model &gt; Markdown</c> precedence.
/// </summary>
public static partial class ModelSourceMerger
{
    private const string DefaultRoot = "ds";

    /// <summary>Merges all authoring sources into one immutable data tree.</summary>
    public static ModelMergeResult Merge(
        IReadOnlyDictionary<string, ModelValue>? modelData,
        SectionAnchorParseResult anchoredMarkdown,
        IReadOnlyDictionary<string, string>? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(anchoredMarkdown);

        var root = new MutableNode();
        var diagnostics = new DiagnosticCollector();
        foreach (var diagnostic in anchoredMarkdown.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        foreach (var section in anchoredMarkdown.Sections)
        {
            var value = ConvertSection(section, diagnostics);
            if (value is null)
            {
                continue;
            }

            var segments = section.Path.Split('.', StringSplitOptions.None);
            if (!TryInsertAnchor(root, segments, value))
            {
                diagnostics.Add(
                    DiagnosticCode.ModelDuplicateSection,
                    ToModelPointer(segments),
                    $"Anchored section '{section.Path}' overlaps another anchored model path.",
                    "Use non-overlapping section paths so one anchor is not the parent or child of another.");
            }
        }

        if (modelData is not null)
        {
            OverlayModelObject(root, modelData, [], diagnostics);
        }

        if (overrides is not null)
        {
            ApplyOverrides(root, overrides, diagnostics);
        }

        return new ModelMergeResult(FreezeObject(root), diagnostics.Items);
    }

    private static ModelValue? ConvertSection(
        AnchoredMarkdownSection section,
        DiagnosticCollector diagnostics)
    {
        if (!section.Attributes.TryGetValue("format", out var format)
            || string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "md", StringComparison.OrdinalIgnoreCase))
        {
            return new InlineMarkdownModelValue(section.Markdown);
        }

        if (string.Equals(format, "table", StringComparison.OrdinalIgnoreCase))
        {
            var conversion = AnchoredTableConverter.Convert(section);
            foreach (var diagnostic in conversion.Diagnostics)
            {
                diagnostics.Add(diagnostic);
            }

            return conversion.Value;
        }

        diagnostics.Add(
            DiagnosticCode.MarkdownInvalidSectionAnchor,
            $"/markdown/lines/{section.AnchorLine}",
            $"Section '{section.Path}' uses unsupported format '{format}'.",
            "Use format=markdown, format=md, format=table, or omit the format attribute.");
        return null;
    }

    private static bool TryInsertAnchor(
        MutableNode root,
        string[] segments,
        ModelValue value)
    {
        var node = root;
        var ancestors = new List<MutableNode> { root };
        for (var index = 0; index < segments.Length; index++)
        {
            if (node.Value is not null)
            {
                return false;
            }

            var segment = segments[index];
            if (!node.Children.TryGetValue(segment, out var child))
            {
                child = new MutableNode();
                node.Children.Add(segment, child);
            }

            node = child;
            ancestors.Add(node);
        }

        if (node.Value is not null || node.Children.Count > 0)
        {
            return false;
        }

        node.Value = value;
        foreach (var ancestor in ancestors)
        {
            ancestor.ContainsAnchoredContent = true;
        }

        return true;
    }

    private static void OverlayModelObject(
        MutableNode destination,
        IReadOnlyDictionary<string, ModelValue> source,
        IReadOnlyList<string> parentPath,
        DiagnosticCollector diagnostics)
    {
        foreach (var property in source)
        {
            if (!destination.Children.TryGetValue(property.Key, out var child))
            {
                child = new MutableNode();
                destination.Children.Add(property.Key, child);
            }

            var path = parentPath.Append(property.Key).ToArray();
            OverlayModelValue(child, property.Value, path, diagnostics);
        }
    }

    private static void OverlayModelValue(
        MutableNode destination,
        ModelValue source,
        IReadOnlyList<string> path,
        DiagnosticCollector diagnostics)
    {
        if (source is ObjectModelValue sourceObject)
        {
            if (destination.Value is not null)
            {
                AddModelOverrideDiagnostic(destination, path, diagnostics);
                destination.Value = null;
            }

            OverlayModelObject(destination, sourceObject.Properties, path, diagnostics);
            destination.ContainsAnchoredContent = destination.Children.Values.Any(
                child => child.ContainsAnchoredContent);
            return;
        }

        AddModelOverrideDiagnostic(destination, path, diagnostics);
        destination.Children.Clear();
        destination.Value = source;
        destination.ContainsAnchoredContent = false;
    }

    private static void AddModelOverrideDiagnostic(
        MutableNode destination,
        IReadOnlyList<string> path,
        DiagnosticCollector diagnostics)
    {
        if (!destination.ContainsAnchoredContent
            && !destination.Children.Values.Any(child => child.ContainsAnchoredContent))
        {
            return;
        }

        var dottedPath = string.Join('.', path);
        diagnostics.Add(
            DiagnosticCode.ModelOverridesMarkdown,
            ToModelPointer(path),
            $"Explicit model value '{dottedPath}' overrides anchored Markdown.",
            $"Remove the '{dottedPath}' value from model JSON to use the Markdown section, or keep it to confirm the override.");
    }

    private static void ApplyOverrides(
        MutableNode root,
        IReadOnlyDictionary<string, string> overrides,
        DiagnosticCollector diagnostics)
    {
        foreach (var item in overrides)
        {
            if (!ModelPathRegex().IsMatch(item.Key))
            {
                diagnostics.Add(
                    DiagnosticCode.ModelSchemaViolation,
                    $"/overrides/{EscapePointerToken(item.Key)}",
                    $"Override path '{item.Key}' is invalid.",
                    "Use a dotted path such as ds.Document.Version with letters, digits, underscores, and dots.");
                continue;
            }

            var resolvedPath = item.Key.Contains('.', StringComparison.Ordinal)
                ? item.Key
                : $"{DefaultRoot}.{item.Key}";
            var segments = resolvedPath.Split('.', StringSplitOptions.None);
            var node = root;
            foreach (var segment in segments)
            {
                if (!node.Children.TryGetValue(segment, out var child))
                {
                    child = new MutableNode();
                    node.Children.Add(segment, child);
                }

                node.Value = null;
                node = child;
            }

            node.Children.Clear();
            node.Value = ParseOverrideValue(item.Value);
            node.ContainsAnchoredContent = false;
        }
    }

    private static PrimitiveModelValue ParseOverrideValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.StartsWith('@'))
        {
            return CreateStringValue(value[1..]);
        }

        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new PrimitiveModelValue(JsonSerializer.SerializeToElement(true));
        }

        if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
        {
            return new PrimitiveModelValue(JsonSerializer.SerializeToElement(false));
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return new PrimitiveModelValue(
                JsonSerializer.SerializeToElement<string?>(null));
        }

        if (JsonNumberRegex().IsMatch(value))
        {
            using var document = JsonDocument.Parse(value);
            return new PrimitiveModelValue(document.RootElement);
        }

        return CreateStringValue(value);
    }

    private static PrimitiveModelValue CreateStringValue(string value) =>
        new(JsonSerializer.SerializeToElement(value));

    private static ReadOnlyDictionary<string, ModelValue> FreezeObject(MutableNode node)
    {
        var values = node.Children.ToDictionary(
            property => property.Key,
            property => FreezeValue(property.Value),
            StringComparer.Ordinal);
        return new ReadOnlyDictionary<string, ModelValue>(values);
    }

    private static ModelValue FreezeValue(MutableNode node)
    {
        if (node.Value is not null)
        {
            return node.Value;
        }

        return new ObjectModelValue(FreezeObject(node));
    }

    private static string ToModelPointer(IReadOnlyList<string> path) =>
        $"/data/{string.Join('/', path.Select(EscapePointerToken))}";

    private static string EscapePointerToken(string token) =>
        token.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);

    [GeneratedRegex(
        "^[A-Za-z_][A-Za-z0-9_.]{0,127}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ModelPathRegex();

    [GeneratedRegex(
        "^-?(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex JsonNumberRegex();

    private sealed class MutableNode
    {
        public Dictionary<string, MutableNode> Children { get; } =
            new(StringComparer.Ordinal);

        public ModelValue? Value { get; set; }

        public bool ContainsAnchoredContent { get; set; }
    }
}
