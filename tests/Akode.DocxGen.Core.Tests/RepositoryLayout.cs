namespace Akode.DocxGen.Core.Tests;

internal static class RepositoryLayout
{
    public static string Root { get; } = FindRoot();

    public static string RelativePath(string path) =>
        Path.GetRelativePath(Root, path).Replace('\\', '/');

    public static bool IsGeneratedPath(string path)
    {
        var relativePath = RelativePath(path);
        return relativePath.StartsWith("artifacts/", StringComparison.Ordinal)
            || relativePath.Contains("/bin/", StringComparison.Ordinal)
            || relativePath.Contains("/obj/", StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DocxGen.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not find DocxGen.sln above '{AppContext.BaseDirectory}'.");
    }
}
