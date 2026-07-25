using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Generated template-specific JSON Schema and provenance.</summary>
public sealed record GenerateSchemaResult(
    string SchemaJson,
    string TemplateId,
    string TemplateVersion,
    string TemplateHash,
    int BindingCount,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets whether schema generation completed without errors.</summary>
    public bool IsSuccess =>
        Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
