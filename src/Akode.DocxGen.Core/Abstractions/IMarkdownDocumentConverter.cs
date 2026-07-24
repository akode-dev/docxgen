using Akode.DocxGen.Core.Pipeline;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Creates a standalone DOCX from renderer-neutral Markdown.</summary>
public interface IMarkdownDocumentConverter
{
    /// <summary>Converts Markdown without a placeholder-bearing template.</summary>
    Task<ConvertResult> ConvertAsync(
        ConvertRequest request,
        CancellationToken cancellationToken = default);
}
