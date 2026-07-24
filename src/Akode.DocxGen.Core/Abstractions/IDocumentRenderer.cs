using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Renders a bound model into a document stream.</summary>
public interface IDocumentRenderer
{
    /// <summary>Renders without taking ownership of the output destination.</summary>
    Task<RenderOutcome> RenderAsync(
        Stream templateDocument,
        BoundModel model,
        RenderOptions options,
        CancellationToken cancellationToken = default);
}
