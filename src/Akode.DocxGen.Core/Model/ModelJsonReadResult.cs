using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Outcome of syntactic, base-schema, and directive model parsing.</summary>
public sealed record ModelJsonReadResult(
    ModelDocument? Model,
    string ModelHash,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets whether a typed model was produced without errors.</summary>
    public bool IsValid =>
        Model is not null
        && Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
