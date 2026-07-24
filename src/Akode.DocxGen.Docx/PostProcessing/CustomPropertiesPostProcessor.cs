using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Utilities;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.VariantTypes;

namespace Akode.DocxGen.Docx.PostProcessing;

/// <summary>Writes deterministic string-valued custom document properties.</summary>
public sealed class CustomPropertiesPostProcessor : IDocumentPostProcessor
{
    private const string PropertyFormatId =
        "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public void Apply(
        Stream document,
        PostProcessOptions options,
        DiagnosticCollector diagnostics)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (options.DocumentProperties.Count == 0)
        {
            return;
        }

        using var package = WordprocessingDocument.Open(
            new NonClosingStream(document),
            true);
        var part = package.CustomFilePropertiesPart
            ?? package.AddCustomFilePropertiesPart();
        part.Properties ??= new Properties();
        foreach (var item in options.DocumentProperties.OrderBy(
                     item => item.Key,
                     StringComparer.Ordinal))
        {
            var existing = part.Properties
                .Elements<CustomDocumentProperty>()
                .FirstOrDefault(property => string.Equals(
                    property.Name?.Value,
                    item.Key,
                    StringComparison.Ordinal));
            var propertyId = existing?.PropertyId?.Value
                ?? NextPropertyId(part.Properties);
            existing?.Remove();
            part.Properties.AppendChild(
                new CustomDocumentProperty(new VTLPWSTR(item.Value))
                {
                    FormatId = PropertyFormatId,
                    PropertyId = propertyId,
                    Name = item.Key,
                });
        }

        part.Properties.Save();
    }

    private static int NextPropertyId(Properties properties) =>
        properties.Elements<CustomDocumentProperty>()
            .Select(property => property.PropertyId?.Value ?? 1)
            .DefaultIfEmpty(1)
            .Max() + 1;
}
