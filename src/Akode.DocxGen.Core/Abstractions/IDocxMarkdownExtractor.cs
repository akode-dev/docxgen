using Akode.DocxGen.Core.Pipeline;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Extracts semantic Markdown and assets from a DOCX document.</summary>
public interface IDocxMarkdownExtractor
{
    /// <summary>Extracts the main document body without reproducing Word layout.</summary>
    Task<ExtractResult> ExtractAsync(
        ExtractRequest request,
        CancellationToken cancellationToken = default);
}
