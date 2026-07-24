using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Conversion;
using Akode.DocxGen.Docx.Extraction;
using Akode.DocxGen.Docx.Inspection;
using Akode.DocxGen.Docx.PostProcessing;
using Akode.DocxGen.Docx.Rendering;
using Akode.DocxGen.Docx.Validation;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Docx.Tests;

public sealed class ReferenceTemplateIntegrationTests
{
    [Fact]
    public async Task RendersAndValidatesTheCompleteReferenceDocument()
    {
        var root = FindRepositoryRoot();
        var templatePath = Path.Combine(root, "templates", "proposal.docx");
        var modelPath = Path.Combine(root, "samples", "model.json");
        var pipeline = CreatePipeline();
        await using var template = File.OpenRead(templatePath);
        await using var model = File.OpenRead(modelPath);

        var result = await pipeline.RenderAsync(
            new RenderRequest(
                new InputArtifact(templatePath, template),
                new InputArtifact(modelPath, model),
                markdown: null,
                Path.Combine(root, "samples"),
                new RenderOptions
                {
                    Culture = "en-US",
                    Strict = true,
                },
                validateOutput: true),
            TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        var validation = result.Validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeTrue();
        result.MarkdownStats.Headings.ShouldBe(6);
        result.MarkdownStats.Tables.ShouldBe(1);
        result.MarkdownStats.Images.ShouldBe(1);
        result.BoundPaths.ShouldContain("ds.Body");
        await using var documentStream = result.Document.ShouldNotBeNull();
        using var document = WordprocessingDocument.Open(documentStream, false);
        var main = document.MainDocumentPart.ShouldNotBeNull();
        var wordDocument = main.Document.ShouldNotBeNull();
        var body = wordDocument.Body.ShouldNotBeNull();
        body.Descendants<Drawing>().ShouldHaveSingleItem();
        body.Descendants<Table>().Count().ShouldBeGreaterThanOrEqualTo(4);
        body.Descendants<ParagraphStyleId>().Count(
            style => style.Val?.Value is "Heading1" or "Heading2")
            .ShouldBe(6);
        body.InnerText.ShouldNotContain("{{");
    }

    [Fact]
    public async Task RendersAFileDirectiveThroughTheImgFormatter()
    {
        using var template = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   template,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(
                new Body(
                    new Paragraph(
                        new Run(new Text("{{ds.Logo}:IMG}"))),
                    new SectionProperties(
                        new PageSize { Width = 12_240U, Height = 15_840U },
                        new PageMargin
                        {
                            Top = 1_440,
                            Right = 1_440U,
                            Bottom = 1_440,
                            Left = 1_440U,
                            Header = 720U,
                            Footer = 720U,
                            Gutter = 0U,
                        })));
        }

        template.Position = 0;
        var imagePath = Path.Combine(
            FindRepositoryRoot(),
            "samples",
            "architecture.svg");
        var asset = new ResolvedAsset(
            imagePath,
            AssetKind.Binary,
            "image/svg+xml",
            await File.ReadAllBytesAsync(
                imagePath,
                TestContext.Current.CancellationToken).ConfigureAwait(true));
        var model = new BoundModel(
            new Dictionary<string, object?>
            {
                ["ds"] = new Dictionary<string, object?>
                {
                    ["Logo"] = asset,
                },
            },
            ["ds.Logo"],
            [],
            new MarkdownStats(0, 0, 0, 0, 0));
        var renderer = new DocxTemplaterRenderer();

        var outcome = await renderer.RenderAsync(
            template,
            model,
            new RenderOptions
            {
                Culture = "en-US",
                Strict = true,
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var output = outcome.Document;
        using var rendered = WordprocessingDocument.Open(output, false);
        var body = rendered.MainDocumentPart.ShouldNotBeNull()
            .Document.ShouldNotBeNull()
            .Body.ShouldNotBeNull();
        body.Descendants<Drawing>().ShouldHaveSingleItem();
        body.InnerText.ShouldNotContain("{{");
    }

    private static DocxGenPipeline CreatePipeline() =>
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

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Akode.DocxGen.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not find Akode.DocxGen.sln above '{AppContext.BaseDirectory}'.");
    }
}
