using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Resolves local model assets under an approved root directory.</summary>
public interface IAssetResolver
{
    /// <summary>Resolves an asset after path and size policy checks.</summary>
    ResolvedAsset Resolve(string relativePath, AssetKind kind);
}
