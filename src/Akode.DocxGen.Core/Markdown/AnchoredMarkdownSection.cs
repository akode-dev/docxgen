using System.Collections.ObjectModel;
using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Markdown;

/// <summary>A normalized Markdown block assigned to one dotted model path.</summary>
public sealed record AnchoredMarkdownSection
{
    /// <summary>Initializes an immutable anchored section.</summary>
    public AnchoredMarkdownSection(
        string path,
        string markdown,
        IReadOnlyDictionary<string, string> attributes,
        int anchorLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentOutOfRangeException.ThrowIfLessThan(anchorLine, 1);

        Path = path;
        Markdown = markdown;
        Attributes = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(
                attributes,
                StringComparer.OrdinalIgnoreCase));
        AnchorLine = anchorLine;
    }

    /// <summary>Gets the resolved case-sensitive dotted model path.</summary>
    public string Path { get; }

    /// <summary>Gets normalized section Markdown without the anchor lines.</summary>
    public string Markdown { get; }

    /// <summary>Gets case-insensitive marker attributes.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>Gets the one-based source line containing the section marker.</summary>
    public int AnchorLine { get; }
}

/// <summary>Outcome of deterministic section-anchor scanning.</summary>
public sealed record SectionAnchorParseResult
{
    /// <summary>Initializes an immutable parser result.</summary>
    public SectionAnchorParseResult(
        IEnumerable<AnchoredMarkdownSection> sections,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Sections = new ReadOnlyCollection<AnchoredMarkdownSection>(sections.ToArray());
        Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics.ToArray());
    }

    /// <summary>Gets sections in source order.</summary>
    public IReadOnlyList<AnchoredMarkdownSection> Sections { get; }

    /// <summary>Gets parser diagnostics in source order.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets whether no blocking parser diagnostic was emitted.</summary>
    public bool IsValid =>
        Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
