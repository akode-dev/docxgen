using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Markdown;

/// <summary>
/// Splits one Markdown source into model-bound sections using standalone
/// HTML-comment markers while ignoring markers inside fenced code blocks.
/// </summary>
public static partial class SectionAnchorParser
{
    private const string DefaultRoot = "ds";

    /// <summary>Parses comment-style anchors and resolves unqualified names under ds.</summary>
    public static SectionAnchorParseResult Parse(string markdown) =>
        Parse(markdown, DefaultRoot);

    /// <summary>Parses comment-style anchors using a caller-selected default root.</summary>
    public static SectionAnchorParseResult Parse(string markdown, string defaultRoot)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (!PathSegmentRegex().IsMatch(defaultRoot))
        {
            throw new ArgumentException(
                "The default root must be a valid model path segment.",
                nameof(defaultRoot));
        }

        var normalized = Normalize(markdown);
        var lines = normalized.Split('\n');
        var sections = new List<AnchoredMarkdownSection>();
        var diagnostics = new DiagnosticCollector();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var current = default(SectionBuilder);
        var fence = default(FenceState);
        var firstPreambleLine = 0;
        var foundSectionAnchor = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var lineNumber = index + 1;

            if (TryToggleFence(line, ref fence))
            {
                current?.Lines.Add(line);
                continue;
            }

            if (fence.IsOpen)
            {
                current?.Lines.Add(line);
                continue;
            }

            if (TryParseSectionMarker(line, out var marker))
            {
                FinalizeSection(current, sections);
                if (!foundSectionAnchor && firstPreambleLine > 0)
                {
                    diagnostics.Add(
                        DiagnosticCode.MarkdownAnchorPreambleIgnored,
                        ToLinePointer(firstPreambleLine),
                        $"Markdown content before the first section anchor was ignored starting on line {firstPreambleLine}.",
                        $"Add a docxgen:section marker before line {firstPreambleLine} or remove the preamble.");
                }

                foundSectionAnchor = true;

                var path = marker.Name.Contains('.', StringComparison.Ordinal)
                    ? marker.Name
                    : $"{defaultRoot}.{marker.Name}";
                var isDuplicate = !paths.Add(path);
                if (isDuplicate)
                {
                    diagnostics.Add(
                        DiagnosticCode.ModelDuplicateSection,
                        ToModelPointer(path),
                        $"Markdown section '{path}' is declared more than once; the duplicate marker is on line {lineNumber}.",
                        $"Keep the first '{path}' section or rename the marker on line {lineNumber}.");
                }

                current = new SectionBuilder(
                    path,
                    marker.Attributes,
                    lineNumber,
                    isDuplicate);
                continue;
            }

            if (EndMarkerRegex().IsMatch(line))
            {
                if (current is null)
                {
                    diagnostics.Add(
                        DiagnosticCode.MarkdownInvalidSectionAnchor,
                        ToLinePointer(lineNumber),
                        $"A docxgen:end marker on line {lineNumber} has no open section.",
                        "Remove the marker or place it after a docxgen:section marker.");
                }

                FinalizeSection(current, sections);
                current = null;
                continue;
            }

            if (DocxGenMarkerPrefixRegex().IsMatch(line))
            {
                diagnostics.Add(
                    DiagnosticCode.MarkdownInvalidSectionAnchor,
                    ToLinePointer(lineNumber),
                    $"The DocxGen marker on line {lineNumber} has invalid syntax.",
                    "Use '<!-- docxgen:section Name key=value -->' or '<!-- docxgen:end -->' on its own line.");
                continue;
            }

            if (current is not null)
            {
                current.Lines.Add(line);
            }
            else if (!foundSectionAnchor
                     && firstPreambleLine == 0
                     && !string.IsNullOrWhiteSpace(line))
            {
                firstPreambleLine = lineNumber;
            }
        }

        FinalizeSection(current, sections);

        return new SectionAnchorParseResult(sections, diagnostics.Items);
    }

    private static string Normalize(string markdown)
    {
        var withoutBom = markdown.Length > 0 && markdown[0] == '\uFEFF'
            ? markdown[1..]
            : markdown;
        return withoutBom.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static bool TryParseSectionMarker(string line, out SectionMarker marker)
    {
        var match = SectionMarkerRegex().Match(line);
        if (!match.Success)
        {
            marker = default;
            return false;
        }

        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var attributeText = match.Groups["attributes"].Value;
        foreach (var token in attributeText.Split(
                     (char[]?)null,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = token.IndexOf('=', StringComparison.Ordinal);
            var key = token[..separator];
            var value = token[(separator + 1)..];
            if (!attributes.TryAdd(key, value))
            {
                marker = default;
                return false;
            }
        }

        marker = new SectionMarker(
            match.Groups["name"].Value,
            new ReadOnlyDictionary<string, string>(attributes));
        return true;
    }

    private static bool TryToggleFence(string line, ref FenceState state)
    {
        var position = 0;
        while (position < line.Length && position < 3 && line[position] == ' ')
        {
            position++;
        }

        if (position >= line.Length || line[position] is not ('`' or '~'))
        {
            return false;
        }

        var character = line[position];
        var end = position;
        while (end < line.Length && line[end] == character)
        {
            end++;
        }

        var length = end - position;
        if (length < 3)
        {
            return false;
        }

        if (!state.IsOpen)
        {
            if (character == '`'
                && line.AsSpan(end).Contains('`'))
            {
                return false;
            }

            state = new FenceState(character, length);
            return true;
        }

        if (state.Character != character
            || length < state.Length
            || !string.IsNullOrWhiteSpace(line[end..]))
        {
            return false;
        }

        state = default;
        return true;
    }

    private static void FinalizeSection(
        SectionBuilder? section,
        List<AnchoredMarkdownSection> sections)
    {
        if (section is null || section.IsDuplicate)
        {
            return;
        }

        sections.Add(
            new AnchoredMarkdownSection(
                section.Path,
                TrimBlock(section.Lines),
                section.Attributes,
                section.AnchorLine));
    }

    private static string TrimBlock(List<string> lines)
    {
        var start = 0;
        while (start < lines.Count && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        var end = lines.Count - 1;
        while (end >= start && string.IsNullOrWhiteSpace(lines[end]))
        {
            end--;
        }

        if (start > end)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = start; index <= end; index++)
        {
            if (index > start)
            {
                builder.Append('\n');
            }

            builder.Append(lines[index]);
        }

        return builder.ToString();
    }

    private static string ToLinePointer(int lineNumber) => $"/markdown/lines/{lineNumber}";

    private static string ToModelPointer(string path) =>
        $"/data/{path.Replace(".", "/", StringComparison.Ordinal)}";

    [GeneratedRegex(
        "^[ \\t]{0,3}<!--[ \\t]*docxgen:section[ \\t]+(?<name>[A-Za-z_][A-Za-z0-9_.]{0,127})(?<attributes>(?:[ \\t]+[A-Za-z][A-Za-z0-9_-]*=[^ \\t]+)*)[ \\t]*-->[ \\t]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SectionMarkerRegex();

    [GeneratedRegex(
        "^[ \\t]{0,3}<!--[ \\t]*docxgen:end[ \\t]*-->[ \\t]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EndMarkerRegex();

    [GeneratedRegex(
        "^[ \\t]{0,3}<!--[ \\t]*docxgen:",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DocxGenMarkerPrefixRegex();

    [GeneratedRegex(
        "^[A-Za-z_][A-Za-z0-9_]{0,127}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex PathSegmentRegex();

    private readonly record struct SectionMarker(
        string Name,
        IReadOnlyDictionary<string, string> Attributes);

    private readonly record struct FenceState(char Character, int Length)
    {
        public bool IsOpen => Length > 0;
    }

    private sealed record SectionBuilder(
        string Path,
        IReadOnlyDictionary<string, string> Attributes,
        int AnchorLine,
        bool IsDuplicate)
    {
        public List<string> Lines { get; } = [];
    }
}
