using System.Text.RegularExpressions;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Utilities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace Akode.DocxGen.Docx.PostProcessing;

/// <summary>Reports unresolved template markers that survived rendering.</summary>
public sealed partial class LeftoverPlaceholderPostProcessor
    : IDocumentPostProcessor
{
    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public void Apply(
        Stream document,
        PostProcessOptions options,
        DiagnosticCollector diagnostics)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnostics);
        using var package = WordprocessingDocument.Open(
            new NonClosingStream(document),
            false);
        var main = package.MainDocumentPart;
        if (main is null)
        {
            return;
        }

        var roots = new List<(string Location, OpenXmlPartRootElement Root)>();
        if (main.Document is not null)
        {
            roots.Add(("main", main.Document));
        }

        foreach (var part in main.HeaderParts)
        {
            if (part.Header is { } header)
            {
                roots.Add(("header", header));
            }
        }

        foreach (var part in main.FooterParts)
        {
            if (part.Footer is { } footer)
            {
                roots.Add(("footer", footer));
            }
        }
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (location, root) in roots)
        {
            foreach (Match match in PlaceholderRegex().Matches(root.InnerText))
            {
                if (emitted.Add($"{location}\0{match.Value}"))
                {
                    diagnostics.Add(
                        DiagnosticCode.LeftoverPlaceholderInOutput,
                        location,
                        $"Unresolved placeholder '{match.Value}' remains in {location}.");
                }
            }
        }
    }

    [GeneratedRegex(
        @"\{\{[^{}\r\n]+\}(?:\}|:[A-Za-z][A-Za-z0-9_-]*(?:\([^)]*\))?\})",
        RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();
}
