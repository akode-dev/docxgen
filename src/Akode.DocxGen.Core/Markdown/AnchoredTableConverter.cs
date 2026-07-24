using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Akode.DocxGen.Core.Markdown;

/// <summary>Converts a format=table anchored section into a model collection.</summary>
public static partial class AnchoredTableConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .Build();

    /// <summary>Converts one anchored GFM pipe table without resolving assets.</summary>
    public static AnchoredTableConversionResult Convert(AnchoredMarkdownSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        var diagnostics = new DiagnosticCollector();
        var pointer = $"/markdown/lines/{section.AnchorLine}";

        if (!section.Attributes.TryGetValue("format", out var format)
            || !string.Equals(format, "table", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(
                DiagnosticCode.MarkdownInvalidTableAnchor,
                pointer,
                $"Section '{section.Path}' is not declared with format=table.",
                "Add format=table to the section marker before requesting table conversion.");
            return new AnchoredTableConversionResult(null, diagnostics.Items);
        }

        if (!TryParseColumns(section, pointer, diagnostics, out var columns))
        {
            return new AnchoredTableConversionResult(null, diagnostics.Items);
        }

        var document = global::Markdig.Markdown.Parse(section.Markdown, Pipeline);
        var blocks = document.ToArray();
        if (blocks.Length != 1 || blocks[0] is not Table table)
        {
            diagnostics.Add(
                DiagnosticCode.MarkdownInvalidTableAnchor,
                pointer,
                $"Section '{section.Path}' must contain exactly one GFM pipe table.",
                "Remove prose and additional blocks, leaving one header row, separator row, and data rows.");
            return new AnchoredTableConversionResult(null, diagnostics.Items);
        }

        var rows = table.OfType<TableRow>().ToArray();
        if (rows.Length == 0 || !rows[0].IsHeader)
        {
            diagnostics.Add(
                DiagnosticCode.MarkdownInvalidTableAnchor,
                pointer,
                $"The table in section '{section.Path}' has no header row.",
                "Add a GFM header row followed by a delimiter row such as '|---|---|'.");
            return new AnchoredTableConversionResult(null, diagnostics.Items);
        }

        var headerCells = rows[0].OfType<TableCell>().ToArray();
        if (headerCells.Length != columns.Count)
        {
            AddWidthDiagnostic(
                section,
                pointer,
                diagnostics,
                "header",
                headerCells.Length,
                columns.Count);
            return new AnchoredTableConversionResult(null, diagnostics.Items);
        }

        var items = new List<ModelValue>();
        for (var rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            var cells = rows[rowIndex].OfType<TableCell>().ToArray();
            if (cells.Length != columns.Count)
            {
                AddWidthDiagnostic(
                    section,
                    pointer,
                    diagnostics,
                    $"data row {rowIndex}",
                    cells.Length,
                    columns.Count);
                continue;
            }

            var properties = new Dictionary<string, ModelValue>(StringComparer.Ordinal);
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                properties.Add(
                    columns[columnIndex],
                    new PlainTextModelValue(GetPlainText(cells[columnIndex])));
            }

            items.Add(new ObjectModelValue(properties));
        }

        return diagnostics.HasErrors
            ? new AnchoredTableConversionResult(null, diagnostics.Items)
            : new AnchoredTableConversionResult(
                new CollectionModelValue(items),
                diagnostics.Items);
    }

    private static bool TryParseColumns(
        AnchoredMarkdownSection section,
        string pointer,
        DiagnosticCollector diagnostics,
        out IReadOnlyList<string> columns)
    {
        if (!section.Attributes.TryGetValue("columns", out var value))
        {
            diagnostics.Add(
                DiagnosticCode.MarkdownInvalidTableAnchor,
                pointer,
                $"Table section '{section.Path}' does not declare columns.",
                "Add a comma-separated columns=Name,Role attribute to the section marker.");
            columns = [];
            return false;
        }

        var parsed = value.Split(',', StringSplitOptions.TrimEntries);
        if (parsed.Length == 0
            || parsed.Any(column => !ColumnNameRegex().IsMatch(column))
            || parsed.Distinct(StringComparer.Ordinal).Count() != parsed.Length)
        {
            diagnostics.Add(
                DiagnosticCode.MarkdownInvalidTableAnchor,
                pointer,
                $"Table section '{section.Path}' has an invalid columns attribute.",
                "Use unique case-sensitive names such as columns=Name,Role,Allocation.");
            columns = [];
            return false;
        }

        columns = new ReadOnlyCollection<string>(parsed);
        return true;
    }

    private static void AddWidthDiagnostic(
        AnchoredMarkdownSection section,
        string pointer,
        DiagnosticCollector diagnostics,
        string row,
        int actual,
        int expected)
    {
        diagnostics.Add(
            DiagnosticCode.MarkdownInvalidTableAnchor,
            pointer,
            $"The {row} in section '{section.Path}' has {actual} cells; columns declares {expected}.",
            $"Make every row contain exactly {expected} pipe-table cells.");
    }

    private static string GetPlainText(TableCell cell)
    {
        var builder = new StringBuilder();
        foreach (var leaf in cell.OfType<LeafBlock>())
        {
            if (leaf.Inline is null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            AppendChildren(leaf.Inline, builder);
        }

        return builder.ToString().Trim();
    }

    private static void AppendChildren(ContainerInline container, StringBuilder builder)
    {
        for (var inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content);
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case AutolinkInline autolink:
                    builder.Append(autolink.Url);
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case HtmlEntityInline entity:
                    builder.Append(entity.Transcoded);
                    break;
                case LinkInline link when link.FirstChild is null:
                    builder.Append(link.Url);
                    break;
                case ContainerInline nested:
                    AppendChildren(nested, builder);
                    break;
            }
        }
    }

    [GeneratedRegex(
        "^[A-Za-z_][A-Za-z0-9_]{0,127}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ColumnNameRegex();
}

/// <summary>Outcome of converting one table anchor into a collection value.</summary>
public sealed record AnchoredTableConversionResult
{
    /// <summary>Initializes an immutable table conversion result.</summary>
    public AnchoredTableConversionResult(
        CollectionModelValue? value,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Value = value;
        Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics.ToArray());
    }

    /// <summary>Gets the converted collection, or null after an error.</summary>
    public CollectionModelValue? Value { get; }

    /// <summary>Gets conversion diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets whether a collection was produced without blocking diagnostics.</summary>
    public bool IsValid =>
        Value is not null
        && Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
