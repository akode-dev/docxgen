using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Validates a generated OOXML package.</summary>
public interface IOoxmlValidator
{
    /// <summary>Validates a document and caps the number of returned errors.</summary>
    ValidationReport Validate(Stream document, int maxErrors = 50);
}
