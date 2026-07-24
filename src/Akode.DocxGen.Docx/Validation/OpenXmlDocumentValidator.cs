using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Docx.Utilities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace Akode.DocxGen.Docx.Validation;

/// <summary>Validates DOCX packages against the Microsoft 365 Open XML rules.</summary>
public sealed class OpenXmlDocumentValidator : IOoxmlValidator
{
    /// <inheritdoc />
    public ValidationReport Validate(Stream document, int maxErrors = 50)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxErrors);
        var diagnostics = new DiagnosticCollector();
        try
        {
            if (document.CanSeek)
            {
                document.Position = 0;
            }

            using var package = WordprocessingDocument.Open(
                new NonClosingStream(document),
                false);
            var validator = new OpenXmlValidator(FileFormatVersions.Microsoft365);
            foreach (var error in validator.Validate(package).Take(maxErrors))
            {
                var path = error.Path?.XPath ?? error.Part?.Uri.ToString();
                diagnostics.Add(
                    DiagnosticCode.OutputInvalidOoxml,
                    path,
                    error.Description,
                    "Correct the reported template or Markdown construct and render again.");
            }
        }
        catch (Exception exception) when (
            exception is OpenXmlPackageException
            or IOException)
        {
            diagnostics.Add(
                DiagnosticCode.OutputInvalidOoxml,
                message: $"The document package could not be opened: {exception.Message}");
        }

        return new ValidationReport(!diagnostics.HasErrors, diagnostics.Items);
    }
}
