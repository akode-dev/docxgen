namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Severity of a machine-readable diagnostic.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Additional information.</summary>
    Information,

    /// <summary>A recoverable issue.</summary>
    Warning,

    /// <summary>An operation-blocking issue.</summary>
    Error,
}
