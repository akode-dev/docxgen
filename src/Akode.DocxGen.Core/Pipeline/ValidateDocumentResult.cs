using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Product and OOXML validation result for an existing document.</summary>
public sealed record ValidateDocumentResult(ValidationReport Validation);
