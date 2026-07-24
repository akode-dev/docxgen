namespace Akode.DocxGen.Core.Security;

/// <summary>Resource limits applied while reading untrusted model assets.</summary>
public sealed record Limits
{
    /// <summary>Gets the default production limits.</summary>
    public static Limits Default { get; } = new();

    /// <summary>Gets the maximum size of one Markdown file.</summary>
    public long MaxMarkdownBytes { get; init; } = 5 * 1024 * 1024;

    /// <summary>Gets the maximum size of one binary asset.</summary>
    public long MaxAssetBytes { get; init; } = 25 * 1024 * 1024;

    /// <summary>Gets the maximum compressed size of a DOCX extraction input.</summary>
    public long MaxDocumentBytes { get; init; } = 100 * 1024 * 1024;

    /// <summary>Gets the maximum decompressed character count of one Open XML part.</summary>
    public long MaxDocumentPartCharacters { get; init; } = 20_000_000;

    /// <summary>Gets the maximum related Open XML part count.</summary>
    public int MaxDocumentParts { get; init; } = 10_000;

    /// <summary>Gets the maximum number of assets extracted from one DOCX.</summary>
    public int MaxExtractedAssets { get; init; } = 1_000;

    /// <summary>Gets the maximum total size of assets extracted from one DOCX.</summary>
    public long MaxExtractedAssetBytes { get; init; } = 100 * 1024 * 1024;

    /// <summary>Gets the maximum recursive model depth.</summary>
    public int MaxModelDepth { get; init; } = 64;

    /// <summary>Gets the maximum number of model values.</summary>
    public int MaxModelValues { get; init; } = 100_000;
}
