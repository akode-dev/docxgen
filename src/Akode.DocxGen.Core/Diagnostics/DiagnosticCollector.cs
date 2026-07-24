namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Collects diagnostics in pipeline order.</summary>
public sealed class DiagnosticCollector
{
    private readonly List<Diagnostic> diagnostics = [];
    private readonly IReadOnlyList<Diagnostic> readOnlyDiagnostics;

    /// <summary>Initializes an empty collector.</summary>
    public DiagnosticCollector()
    {
        readOnlyDiagnostics = diagnostics.AsReadOnly();
    }

    /// <summary>Gets collected diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Items => readOnlyDiagnostics;

    /// <summary>Gets whether any collected diagnostic is an error.</summary>
    public bool HasErrors => diagnostics.Exists(
        diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>Adds a diagnostic.</summary>
    public void Add(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        diagnostics.Add(diagnostic);
    }

    /// <summary>Creates and adds a registered diagnostic.</summary>
    public Diagnostic Add(
        string code,
        string? path = null,
        string? message = null,
        string? hint = null)
    {
        var diagnostic = DiagnosticRegistry.Create(code, path, message, hint);
        diagnostics.Add(diagnostic);
        return diagnostic;
    }
}
