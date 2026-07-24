using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Applies deterministic OOXML changes after rendering.</summary>
public interface IDocumentPostProcessor
{
    /// <summary>Gets the stable execution order.</summary>
    int Order { get; }

    /// <summary>Applies a post-render operation to an editable document stream.</summary>
    void Apply(
        Stream document,
        PostProcessOptions options,
        DiagnosticCollector diagnostics);
}
