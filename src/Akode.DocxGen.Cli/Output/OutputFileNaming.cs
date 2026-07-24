using System.Text.RegularExpressions;

namespace Akode.DocxGen.Cli.Output;

internal static partial class OutputFileNaming
{
    public static string AppendVersion(string path, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var normalized = version.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        normalized = InvalidVersionCharacterRegex().Replace(normalized, "-")
            .Trim('-', '.');
        if (normalized.Length == 0)
        {
            throw new ArgumentException(
                "The document version cannot be normalized for a file name.",
                nameof(version));
        }

        var directory = Path.GetDirectoryName(path);
        var extension = Path.GetExtension(path);
        var stem = Path.GetFileNameWithoutExtension(path);
        var suffix = $"-v{normalized}";
        var fileName = stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? $"{stem}{extension}"
            : $"{stem}{suffix}{extension}";
        return directory is null ? fileName : Path.Combine(directory, fileName);
    }

    [GeneratedRegex(
        @"[^A-Za-z0-9._-]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex InvalidVersionCharacterRegex();
}
