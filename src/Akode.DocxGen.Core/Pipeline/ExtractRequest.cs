namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for semantic DOCX-to-Markdown extraction.</summary>
public sealed record ExtractRequest
{
    /// <summary>Initializes an extraction request.</summary>
    public ExtractRequest(
        InputArtifact document,
        string imagePathPrefix)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePathPrefix);

        Document = document;
        ImagePathPrefix = imagePathPrefix
            .Replace('\\', '/')
            .TrimEnd('/');
    }

    /// <summary>Gets the input DOCX document.</summary>
    public InputArtifact Document { get; }

    /// <summary>Gets the relative Markdown path prefix used for extracted images.</summary>
    public string ImagePathPrefix { get; }
}
