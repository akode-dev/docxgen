namespace Akode.DocxGen.Core.Security;

/// <summary>Resolves relative paths while preventing traversal outside a root.</summary>
public static class PathGuard
{
    /// <summary>Returns a contained absolute path or throws when containment fails.</summary>
    public static string ResolveContained(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException(
                "Asset paths must be relative to the configured root.",
                nameof(relativePath));
        }

        var fullRoot = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(relativePath, fullRoot);
        var relative = Path.GetRelativePath(fullRoot, candidate);
        if (relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new ArgumentException(
                "The asset path resolves outside the configured root.",
                nameof(relativePath));
        }

        return candidate;
    }
}
