using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Security;

/// <summary>Loads bounded local assets below one immutable root directory.</summary>
public sealed class LocalAssetResolver : IAssetResolver, IRemoteAssetResolver
{
    private readonly string root;
    private readonly Limits limits;

    /// <summary>Initializes a resolver for one approved root.</summary>
    public LocalAssetResolver(string root, Limits? limits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
        this.limits = limits ?? Limits.Default;
    }

    /// <inheritdoc />
    public ResolvedAsset Resolve(string relativePath, AssetKind kind)
    {
        string fullPath;
        try
        {
            fullPath = PathGuard.ResolveContained(root, relativePath);
        }
        catch (ArgumentException exception)
        {
            throw new AssetResolutionException(
                DiagnosticRegistry.Create(
                    DiagnosticCode.AssetOutsideRoot,
                    relativePath,
                    $"Asset '{relativePath}' resolves outside the approved root '{root}'."),
                exception);
        }

        if (!File.Exists(fullPath))
        {
            throw new AssetResolutionException(
                DiagnosticRegistry.Create(
                    DiagnosticCode.AssetNotFound,
                    relativePath,
                    $"Asset '{relativePath}' does not exist.",
                    "Create the referenced file below the assets directory or correct its relative path."));
        }

        var file = new FileInfo(fullPath);
        var maximum = kind == AssetKind.Markdown
            ? limits.MaxMarkdownBytes
            : limits.MaxAssetBytes;
        if (file.Length > maximum)
        {
            throw new AssetResolutionException(
                DiagnosticRegistry.Create(
                    DiagnosticCode.AssetTooLarge,
                    relativePath,
                    $"Asset '{relativePath}' is {file.Length} bytes; the limit is {maximum} bytes."));
        }

        return new ResolvedAsset(
            fullPath,
            kind,
            GetMediaType(file.Extension),
            File.ReadAllBytes(fullPath));
    }

    /// <inheritdoc />
    public ResolvedAsset ResolveRemote(Uri uri, AssetKind kind) =>
        RemoteAssetDownloader.Resolve(uri, kind, limits);

    private static string GetMediaType(string extension) =>
        extension.ToUpperInvariant() switch
        {
            ".MD" or ".MARKDOWN" => "text/markdown",
            ".PNG" => "image/png",
            ".JPG" or ".JPEG" => "image/jpeg",
            ".GIF" => "image/gif",
            ".BMP" => "image/bmp",
            ".SVG" => "image/svg+xml",
            _ => "application/octet-stream",
        };
}
