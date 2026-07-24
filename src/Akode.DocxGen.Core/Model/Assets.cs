namespace Akode.DocxGen.Core.Model;

/// <summary>Supported local asset kinds.</summary>
public enum AssetKind
{
    /// <summary>A Markdown source file.</summary>
    Markdown,

    /// <summary>An image file.</summary>
    Image,

    /// <summary>Another binary asset explicitly supported by a formatter.</summary>
    Binary,
}

/// <summary>A resolved local asset.</summary>
public sealed record ResolvedAsset(
    string FullPath,
    AssetKind Kind,
    string MediaType,
    ReadOnlyMemory<byte> Content);
