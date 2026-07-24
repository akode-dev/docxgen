using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Template-less Markdown conversion result before atomic writing.</summary>
public sealed record ConvertResult(
    Stream Document,
    MarkdownStats MarkdownStats,
    ValidationReport? Validation,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets whether conversion completed without error diagnostics.</summary>
    public bool IsSuccess =>
        Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
