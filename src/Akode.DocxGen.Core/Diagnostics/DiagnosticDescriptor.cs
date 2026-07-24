namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Immutable metadata for one stable diagnostic code.</summary>
public sealed record DiagnosticDescriptor
{
    /// <summary>Initializes diagnostic metadata.</summary>
    /// <param name="code">Stable public diagnostic code.</param>
    /// <param name="severity">Default severity.</param>
    /// <param name="defaultMessage">Default human-readable message.</param>
    /// <param name="defaultHint">Default actionable remediation.</param>
    public DiagnosticDescriptor(
        string code,
        DiagnosticSeverity severity,
        string defaultMessage,
        string defaultHint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultHint);

        Code = code;
        Severity = severity;
        DefaultMessage = defaultMessage;
        DefaultHint = defaultHint;
    }

    /// <summary>Gets the stable public diagnostic code.</summary>
    public string Code { get; }

    /// <summary>Gets the default severity.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Gets the default human-readable message.</summary>
    public string DefaultMessage { get; }

    /// <summary>Gets the default actionable remediation.</summary>
    public string DefaultHint { get; }
}
