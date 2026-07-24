namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Collects diagnostics in pipeline order.</summary>
public sealed class DiagnosticCollector
{
    private readonly List<Diagnostic> diagnostics = [];

    /// <summary>Gets collected diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Items => diagnostics;

    /// <summary>Adds a diagnostic.</summary>
    public void Add(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        diagnostics.Add(diagnostic);
    }
}
