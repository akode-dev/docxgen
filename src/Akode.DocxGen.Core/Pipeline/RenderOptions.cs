namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Options that influence model preprocessing and rendering.</summary>
public sealed record RenderOptions
{
    /// <summary>Gets the formatting culture.</summary>
    public required string Culture { get; init; } = "en-US";

    /// <summary>Gets whether every required placeholder must be bound.</summary>
    public required bool Strict { get; init; } = true;

    /// <summary>Gets the heading level offset applied to Markdown headings.</summary>
    public int HeadingOffset { get; init; }

    /// <summary>Gets whether raw HTML is allowed.</summary>
    public bool AllowRawHtml { get; init; }

    /// <summary>Gets whether remote images are allowed.</summary>
    public bool AllowRemoteImages { get; init; }

    /// <summary>Gets whether Word should update fields when the document opens.</summary>
    public bool UpdateFieldsOnOpen { get; init; } = true;
}
