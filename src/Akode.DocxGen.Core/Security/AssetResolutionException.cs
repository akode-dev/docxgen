using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Security;

/// <summary>An expected local-asset policy failure with a public diagnostic.</summary>
public sealed class AssetResolutionException : Exception
{
    /// <summary>Initializes a generic asset resolution failure.</summary>
    public AssetResolutionException()
        : this("An asset could not be resolved.")
    {
    }

    /// <summary>Initializes an asset resolution failure with a message.</summary>
    public AssetResolutionException(string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Diagnostic = DiagnosticRegistry.Create(
            DiagnosticCode.AssetNotFound,
            message: message);
    }

    /// <summary>Initializes an asset resolution failure with an inner exception.</summary>
    public AssetResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(innerException);
        Diagnostic = DiagnosticRegistry.Create(
            DiagnosticCode.AssetNotFound,
            message: message);
    }

    /// <summary>Initializes an asset policy failure.</summary>
    public AssetResolutionException(Diagnostic diagnostic, Exception? innerException = null)
        : base(diagnostic?.Message, innerException)
    {
        Diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
    }

    /// <summary>Gets the machine-readable failure.</summary>
    public Diagnostic Diagnostic { get; }
}
