using System.Collections.ObjectModel;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Markdown;

/// <summary>Immutable, renderer-neutral Markdown produced by Core.</summary>
public sealed record MarkdownContent
{
    /// <summary>Initializes parsed Markdown.</summary>
    public MarkdownContent(
        IEnumerable<MarkdownBlockNode> blocks,
        MarkdownStats stats,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Blocks = new ReadOnlyCollection<MarkdownBlockNode>(blocks.ToArray());
        Stats = stats;
        Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics.ToArray());
    }

    /// <summary>Gets top-level block nodes.</summary>
    public IReadOnlyList<MarkdownBlockNode> Blocks { get; }

    /// <summary>Gets construct counts.</summary>
    public MarkdownStats Stats { get; }

    /// <summary>Gets preprocessing diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}

/// <summary>Base type for renderer-neutral Markdown blocks.</summary>
public abstract record MarkdownBlockNode;

/// <summary>A paragraph containing inline content.</summary>
public sealed record MarkdownParagraphNode(IReadOnlyList<MarkdownInlineNode> Inlines)
    : MarkdownBlockNode;

/// <summary>A semantic heading.</summary>
public sealed record MarkdownHeadingNode(
    int Level,
    IReadOnlyList<MarkdownInlineNode> Inlines)
    : MarkdownBlockNode;

/// <summary>A fenced or indented code block.</summary>
public sealed record MarkdownCodeBlockNode(string Code, string? Language)
    : MarkdownBlockNode;

/// <summary>A quoted group of blocks.</summary>
public sealed record MarkdownQuoteNode(IReadOnlyList<MarkdownBlockNode> Blocks)
    : MarkdownBlockNode;

/// <summary>An ordered or unordered list.</summary>
public sealed record MarkdownListNode(
    bool Ordered,
    int Start,
    IReadOnlyList<MarkdownListItemNode> Items)
    : MarkdownBlockNode;

/// <summary>One list item's block content.</summary>
public sealed record MarkdownListItemNode(IReadOnlyList<MarkdownBlockNode> Blocks);

/// <summary>A Markdown table with explicit header and body rows.</summary>
public sealed record MarkdownTableNode(
    IReadOnlyList<IReadOnlyList<MarkdownInlineNode>> Header,
    IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkdownInlineNode>>> Rows)
    : MarkdownBlockNode;

/// <summary>A thematic separator.</summary>
public sealed record MarkdownHorizontalRuleNode : MarkdownBlockNode;

/// <summary>Base type for renderer-neutral inline Markdown.</summary>
public abstract record MarkdownInlineNode;

/// <summary>Literal text.</summary>
public sealed record MarkdownTextNode(string Text) : MarkdownInlineNode;

/// <summary>Emphasized inline children.</summary>
public sealed record MarkdownEmphasisNode(
    bool Bold,
    bool Italic,
    bool Strikethrough,
    IReadOnlyList<MarkdownInlineNode> Children)
    : MarkdownInlineNode;

/// <summary>Inline code.</summary>
public sealed record MarkdownCodeInlineNode(string Code) : MarkdownInlineNode;

/// <summary>A clickable hyperlink.</summary>
public sealed record MarkdownLinkNode(
    Uri Url,
    string? Title,
    IReadOnlyList<MarkdownInlineNode> Children)
    : MarkdownInlineNode;

/// <summary>A resolved image with accessible alternative text.</summary>
public sealed record MarkdownImageNode(
    ResolvedAsset Asset,
    string AlternativeText,
    string? Title)
    : MarkdownInlineNode;

/// <summary>An explicit or soft line break.</summary>
public sealed record MarkdownBreakNode(bool Hard) : MarkdownInlineNode;
