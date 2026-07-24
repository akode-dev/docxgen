using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Result of full model preflight without document generation.</summary>
public sealed record ValidateModelResult(
    string TemplateHash,
    string ModelHash,
    BoundModel? Model,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets whether preflight produced a bound model without errors.</summary>
    public bool IsValid =>
        Model is not null
        && Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
