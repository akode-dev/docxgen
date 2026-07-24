namespace Akode.DocxGen.Core.Model;

/// <summary>A validated and asset-resolved data model ready for rendering.</summary>
public sealed record BoundModel(
    IReadOnlyDictionary<string, object?> Roots,
    IReadOnlyList<string> BoundPaths,
    IReadOnlyList<string> UnboundPaths,
    MarkdownStats MarkdownStats)
{
    /// <summary>Gets the optional business document version used for output naming.</summary>
    public string? DocumentVersion { get; init; }
}

/// <summary>Summary of Markdown constructs processed during a render.</summary>
public sealed record MarkdownStats(
    int Sections,
    int Headings,
    int Tables,
    int Images,
    int CodeBlocks)
{
    /// <summary>An empty statistics instance.</summary>
    public static MarkdownStats Empty { get; } = new(0, 0, 0, 0, 0);
}
