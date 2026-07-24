namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for validation of an existing DOCX package.</summary>
public sealed record ValidateDocumentRequest
{
    /// <summary>Initializes an existing-document validation request.</summary>
    public ValidateDocumentRequest(InputArtifact document, int maxErrors = 50)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxErrors);

        Document = document;
        MaxErrors = maxErrors;
    }

    /// <summary>Gets the DOCX package to validate.</summary>
    public InputArtifact Document { get; }

    /// <summary>Gets the maximum number of returned errors.</summary>
    public int MaxErrors { get; }
}
