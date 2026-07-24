using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Semantic DOCX extraction result before atomic file writing.</summary>
public sealed record ExtractResult(
    string Markdown,
    IReadOnlyList<ExtractedAsset> Assets,
    DocxExtractionStats Stats,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets whether extraction completed without error diagnostics.</summary>
    public bool IsSuccess =>
        Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}

/// <summary>An embedded DOCX asset extracted into a safe relative file name.</summary>
public sealed record ExtractedAsset
{
    /// <summary>Initializes an immutable extracted asset.</summary>
    public ExtractedAsset(
        string fileName,
        string mediaType,
        ReadOnlyMemory<byte> content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        if (Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException(
                "An extracted asset name cannot contain a directory.",
                nameof(fileName));
        }

        FileName = fileName;
        MediaType = mediaType;
        Content = content.ToArray();
    }

    /// <summary>Gets the safe output file name.</summary>
    public string FileName { get; }

    /// <summary>Gets the package media type.</summary>
    public string MediaType { get; }

    /// <summary>Gets an immutable copy of the asset bytes.</summary>
    public ReadOnlyMemory<byte> Content { get; }
}

/// <summary>Counts semantic structures extracted from a DOCX main body.</summary>
public sealed record DocxExtractionStats(
    int Paragraphs,
    int Headings,
    int ListItems,
    int Tables,
    int Images)
{
    /// <summary>Gets an empty extraction summary.</summary>
    public static DocxExtractionStats Empty { get; } = new(0, 0, 0, 0, 0);
}
