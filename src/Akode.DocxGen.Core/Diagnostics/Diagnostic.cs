namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>A stable, machine-readable issue with a remediation hint.</summary>
public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    string Hint,
    string? Path = null);
