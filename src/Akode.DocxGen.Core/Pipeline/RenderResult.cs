using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Complete pipeline result before output naming and atomic writing.</summary>
public sealed record RenderResult(
    Stream? Document,
    string TemplateHash,
    string? ModelHash,
    IReadOnlyList<string> BoundPaths,
    IReadOnlyList<string> UnboundPaths,
    MarkdownStats MarkdownStats,
    ValidationReport? Validation,
    IReadOnlyList<Diagnostic> Diagnostics,
    bool DryRun)
{
    /// <summary>Gets the optional business document version from the bound model.</summary>
    public string? DocumentVersion { get; init; }

    /// <summary>Gets whether the pipeline completed without error diagnostics.</summary>
    public bool IsSuccess =>
        Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
