namespace Akode.DocxGen.Core.Tests;

internal static class RepositoryLayout
{
    public static string Root { get; } = FindRoot();

    public static string RelativePath(string path) =>
        Path.GetRelativePath(Root, path).Replace('\\', '/');

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Akode.DocxGen.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not find Akode.DocxGen.sln above '{AppContext.BaseDirectory}'.");
    }
}
