using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Conversion;
using Akode.DocxGen.Docx.Extraction;
using Akode.DocxGen.Docx.Inspection;
using Akode.DocxGen.Docx.PostProcessing;
using Akode.DocxGen.Docx.Rendering;
using Akode.DocxGen.Docx.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Akode.DocxGen.Cli;

internal static class CompositionRoot
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemplateInspector, DocxTemplateInspector>();
        services.AddSingleton<IDocumentRenderer, DocxTemplaterRenderer>();
        services.AddSingleton<IMarkdownDocumentConverter, DocxMarkdownConverter>();
        services.AddSingleton<IDocxMarkdownExtractor, DocxMarkdownExtractor>();
        services.AddSingleton<IOoxmlValidator, OpenXmlDocumentValidator>();
        services.AddSingleton<IDocumentPostProcessor, UpdateFieldsPostProcessor>();
        services.AddSingleton<IDocumentPostProcessor, CustomPropertiesPostProcessor>();
        services.AddSingleton<IDocumentPostProcessor, LeftoverPlaceholderPostProcessor>();
        services.AddSingleton<DocxGenPipeline>();
        return services.BuildServiceProvider();
    }
}
