using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Resolves explicitly allowed remote assets under bounded policy.</summary>
public interface IRemoteAssetResolver
{
    /// <summary>Downloads one approved remote asset.</summary>
    ResolvedAsset ResolveRemote(Uri uri, AssetKind kind);
}
