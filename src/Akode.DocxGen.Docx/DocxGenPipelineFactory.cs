using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Conversion;
using Akode.DocxGen.Docx.Extraction;
using Akode.DocxGen.Docx.Inspection;
using Akode.DocxGen.Docx.PostProcessing;
using Akode.DocxGen.Docx.Rendering;
using Akode.DocxGen.Docx.Validation;

namespace Akode.DocxGen;

/// <summary>Creates ready-to-use DocxGen pipelines for application hosts.</summary>
public static class DocxGenPipelineFactory
{
    /// <summary>
    /// Creates the default pipeline for template inspection, rendering,
    /// conversion, extraction, and validation.
    /// </summary>
    /// <returns>A fully composed pipeline with the supported DOCX adapter.</returns>
    public static DocxGenPipeline CreatePipeline() =>
        new(
            new DocxTemplateInspector(),
            new DocxTemplaterRenderer(),
            new DocxMarkdownConverter(),
            new DocxMarkdownExtractor(),
            new OpenXmlDocumentValidator(),
            new IDocumentPostProcessor[]
            {
                new UpdateFieldsPostProcessor(),
                new CustomPropertiesPostProcessor(),
                new LeftoverPlaceholderPostProcessor(),
            });
}
