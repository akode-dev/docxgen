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

    /// <summary>Gets the maximum recursive model depth.</summary>
    public int MaxModelDepth { get; init; } = 64;

    /// <summary>Gets the maximum number of model values.</summary>
    public int MaxModelValues { get; init; } = 100_000;
}
