using System.Text;
using System.Text.Json;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
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
using Json.Schema;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Docx.Tests;

public sealed class TemplateSchemaGenerationTests
{
    [Fact]
    public async Task GeneratesSchemaForTemplateWithOneScalarBinding()
    {
        var templateBytes = CreateTemplate(
            new Paragraph(new Run(new Text("{{ds.Title}}"))));
        var pipeline = CreatePipeline();
        using var template = new MemoryStream(templateBytes);

        var generated = await pipeline.GenerateSchemaAsync(
            new GenerateSchemaRequest(
                new InputArtifact("Документ.docx", template)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        generated.IsSuccess.ShouldBeTrue();
        generated.TemplateId.ShouldBe("template");
        generated.BindingCount.ShouldBe(1);
        using var schema = JsonDocument.Parse(generated.SchemaJson);
        schema.RootElement
            .GetProperty("properties")
            .GetProperty("data")
            .GetProperty("properties")
            .GetProperty("ds")
            .GetProperty("properties")
            .GetProperty("Title")
            .GetProperty("$ref")
            .GetString()
            .ShouldBe("#/$defs/text");
        schema.RootElement
            .GetProperty("properties")
            .GetProperty("data")
            .TryGetProperty("required", out _)
            .ShouldBeFalse();
        schema.RootElement
            .GetProperty("properties")
            .GetProperty("data")
            .GetProperty("properties")
            .GetProperty("ds")
            .TryGetProperty("required", out _)
            .ShouldBeFalse();
    }

    [Fact]
    public async Task GeneratesValidContractAndRendersMinimalMarkdownTemplate()
    {
        var templateBytes = CreateTemplate(
            new Paragraph(new Run(new Text("{{ds.Body}:MD}"))));
        var pipeline = CreatePipeline();
        using var generationTemplate = new MemoryStream(templateBytes);

        var generated = await pipeline.GenerateSchemaAsync(
            new GenerateSchemaRequest(
                new InputArtifact("minimal.docx", generationTemplate),
                "minimal",
                "2.0.0"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        generated.IsSuccess.ShouldBeTrue();
        generated.BindingCount.ShouldBe(1);
        using var schemaDocument = JsonDocument.Parse(generated.SchemaJson);
        var bodySchema = schemaDocument.RootElement
            .GetProperty("properties")
            .GetProperty("data")
            .GetProperty("properties")
            .GetProperty("ds")
            .GetProperty("properties")
            .GetProperty("Body");
        bodySchema.GetProperty("$ref").GetString().ShouldBe("#/$defs/markdown");

        const string modelJson = """
            {
              "modelVersion": "1.0",
              "template": {
                "id": "minimal",
                "version": "2.0.0"
              },
              "data": {
                "ds": {
                  "Body": {
                    "$md": "# Generated heading\n\nA generated paragraph."
                  }
                }
              }
            }
            """;
        var schema = JsonSchema.FromText(
            generated.SchemaJson,
            new BuildOptions
            {
                SchemaRegistry = new SchemaRegistry(),
            });
        using var validModel = JsonDocument.Parse(modelJson);
        schema.Evaluate(validModel.RootElement).IsValid.ShouldBeTrue();
        using var modelWithoutBody = JsonDocument.Parse(
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "minimal", "version": "2.0.0" },
              "data": { "ds": {} }
            }
            """);
        schema.Evaluate(modelWithoutBody.RootElement).IsValid.ShouldBeTrue();

        var directory = CreateTemporaryDirectory();
        try
        {
            var templatePath = Path.Combine(directory, "minimal.docx");
            await File.WriteAllBytesAsync(
                templatePath,
                templateBytes,
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await File.WriteAllTextAsync(
                Path.ChangeExtension(templatePath, ".schema.json"),
                generated.SchemaJson,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await using var renderTemplate = File.OpenRead(templatePath);
            using var renderModel = new MemoryStream(
                Encoding.UTF8.GetBytes(modelJson));

            var rendered = await pipeline.RenderAsync(
                new RenderRequest(
                    new InputArtifact(templatePath, renderTemplate),
                    new InputArtifact("model.json", renderModel),
                    markdown: null,
                    directory,
                    new RenderOptions
                    {
                        Culture = "en-US",
                        Strict = true,
                    }),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            rendered.IsSuccess.ShouldBeTrue(
                string.Join(
                    Environment.NewLine,
                    rendered.Diagnostics.Select(
                        item => $"{item.Code}: {item.Message} {item.Hint}")));
            await using var output = rendered.Document.ShouldNotBeNull();
            using var document = WordprocessingDocument.Open(output, false);
            var body = document.MainDocumentPart.ShouldNotBeNull()
                .Document.ShouldNotBeNull()
                .Body.ShouldNotBeNull();
            body.InnerText.ShouldContain("Generated heading");
            body.InnerText.ShouldContain("A generated paragraph.");
            body.InnerText.ShouldNotContain("{{");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AdjacentSchemaCanDeliberatelyRequireSelectedPlaceholders()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var templatePath = Path.Combine(directory, "governed.docx");
            await File.WriteAllBytesAsync(
                templatePath,
                CreateTemplate(
                    Paragraph("{{ds.Title}}"),
                    Paragraph("{{ds.Description}}")),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await File.WriteAllTextAsync(
                Path.ChangeExtension(templatePath, ".schema.json"),
                """
                {
                  "type": "object",
                  "properties": {
                    "data": {
                      "required": ["ds"],
                      "properties": {
                        "ds": {
                          "type": "object",
                          "required": ["Title"],
                          "properties": {
                            "Title": { "type": "string", "minLength": 1 },
                            "Description": { "type": "string", "minLength": 1 }
                          }
                        }
                      }
                    }
                  }
                }
                """,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await using var template = File.OpenRead(templatePath);

            var inspection = await CreatePipeline().InspectAsync(
                new InspectRequest(
                    new InputArtifact(templatePath, template)),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            inspection.Schema.Placeholders
                .Single(placeholder => placeholder.Path == "ds.Title")
                .Required.ShouldBeTrue();
            inspection.Schema.Placeholders
                .Single(placeholder => placeholder.Path == "ds.Description")
                .Required.ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoversNestedCollectionsAndBindingsAcrossDocumentParts()
    {
        var templateBytes = CreateComplexTemplate();
        var inspector = new DocxTemplateInspector();
        using var inspectionStream = new MemoryStream(templateBytes);

        var inspection = inspector.Inspect(
            inspectionStream,
            TestContext.Current.CancellationToken);

        inspection.Diagnostics
            .Where(item => item.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty();
        var dataSource = inspection.Roots.ShouldHaveSingleItem();
        dataSource.Name.ShouldBe("ds");
        var document = dataSource.Properties.Single(item => item.Name == "Document");
        document.Properties.Single(item => item.Name == "Logo")
            .Kind.ShouldBe(ModelValueKind.Binary);
        document.Properties.ShouldContain(item => item.Name == "Summary");
        document.Properties.ShouldContain(item => item.Name == "Status");
        var sections = dataSource.Properties.Single(item => item.Name == "Sections");
        sections.Kind.ShouldBe(ModelValueKind.Collection);
        var sectionItem = sections.Item.ShouldNotBeNull();
        var blocks = sectionItem.Properties.Single(item => item.Name == "Blocks");
        blocks.Kind.ShouldBe(ModelValueKind.Collection);
        blocks.Item.ShouldNotBeNull()
            .Properties.Single(item => item.Name == "Content")
            .Kind.ShouldBe(ModelValueKind.Markdown);
        dataSource.Properties.ShouldContain(item => item.Name == "Header");
        dataSource.Properties.ShouldContain(item => item.Name == "Footer");
        dataSource.Properties.ShouldContain(item => item.Name == "ShowAppendix");
        dataSource.Properties.ShouldContain(item => item.Name == "Appendix");
        dataSource.Properties.ShouldContain(item => item.Name == "DraftNote");
        dataSource.Properties.ShouldContain(item => item.Name == "FinalNote");

        var pipeline = CreatePipeline();
        using var generationTemplate = new MemoryStream(templateBytes);
        var generated = await pipeline.GenerateSchemaAsync(
            new GenerateSchemaRequest(
                new InputArtifact("complex-template.docx", generationTemplate)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        generated.IsSuccess.ShouldBeTrue();
        generated.TemplateId.ShouldBe("complex-template");
        using var generatedDocument = JsonDocument.Parse(generated.SchemaJson);
        var dsSchema = generatedDocument.RootElement
            .GetProperty("properties")
            .GetProperty("data")
            .GetProperty("properties")
            .GetProperty("ds");
        var dsProperties = dsSchema.GetProperty("properties");
        dsProperties.GetProperty("Sections")
            .GetProperty("type")
            .GetString()
            .ShouldBe("array");
        dsProperties.GetProperty("Sections")
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("Blocks")
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("Content")
            .GetProperty("$ref")
            .GetString()
            .ShouldBe("#/$defs/markdown");

        using var scaffoldTemplate = new MemoryStream(templateBytes);
        var scaffold = await pipeline.ScaffoldModelAsync(
            new ScaffoldModelRequest(
                new InputArtifact("complex-template.docx", scaffoldTemplate),
                withMarkdownStubs: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var scaffoldDocument = JsonDocument.Parse(scaffold.ModelJson);
        var scaffoldData = scaffoldDocument.RootElement
            .GetProperty("data")
            .GetProperty("ds");
        scaffoldData.GetProperty("Sections")
            .EnumerateArray()
            .Single()
            .GetProperty("Blocks")
            .EnumerateArray()
            .Single()
            .GetProperty("Content")
            .TryGetProperty("$mdFile", out _)
            .ShouldBeTrue();
        scaffold.MarkdownStubs.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ReportsTemplateWithoutBindings()
    {
        var templateBytes = CreateTemplate(
            new Paragraph(new Run(new Text("Static content"))));
        var pipeline = CreatePipeline();
        using var template = new MemoryStream(templateBytes);

        var generated = await pipeline.GenerateSchemaAsync(
            new GenerateSchemaRequest(
                new InputArtifact("static.docx", template)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        generated.IsSuccess.ShouldBeFalse();
        generated.SchemaJson.ShouldBeEmpty();
        generated.Diagnostics.ShouldContain(
            item => item.Code == "E-SCH-001");
    }

    [Fact]
    public async Task ReportsMalformedTemplateBlockAsDiagnostic()
    {
        var templateBytes = CreateTemplate(
            new Paragraph(new Run(new Text("{{#ds.Items}}"))));
        var pipeline = CreatePipeline();
        using var template = new MemoryStream(templateBytes);

        var generated = await pipeline.GenerateSchemaAsync(
            new GenerateSchemaRequest(
                new InputArtifact("malformed.docx", template)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        generated.IsSuccess.ShouldBeFalse();
        generated.Diagnostics.ShouldContain(
            item => item.Code == "E-TPL-003");
    }

    private static byte[] CreateComplexTemplate()
    {
        using var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            var body = new Body(
                Paragraph("{{ds.Document.Title}}"),
                Paragraph("{{ds.Document.Logo}:IMG}"),
                Paragraph("{{#ds.Sections}}"),
                Paragraph("{{.Title}}"),
                Paragraph("{{#.Blocks}}"),
                Paragraph("{{.Content}:MD}"),
                Paragraph("{{/.Blocks}}"),
                Paragraph("{{/ds.Sections}}"),
                Paragraph("{{(ds.Document.Summary.ToUpper())}}"),
                Paragraph("{{#switch: ds.Document.Status}}"),
                Paragraph("{{#case: 'Draft'}}"),
                Paragraph("{{ds.DraftNote}}"),
                Paragraph("{{#default}}"),
                Paragraph("{{ds.FinalNote}}"),
                Paragraph("{{/}}"),
                Paragraph("{{/}}"),
                Paragraph("{?{ds.ShowAppendix}}"),
                Paragraph("{{ds.Appendix.Title}}"),
                Paragraph("{{/}}"));
            main.Document = new Document(body);

            var headerPart = main.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(Paragraph("{{ds.Header}}"));
            var footerPart = main.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(Paragraph("{{ds.Footer}}"));
            body.Append(
                new SectionProperties(
                    new HeaderReference
                    {
                        Type = HeaderFooterValues.Default,
                        Id = main.GetIdOfPart(headerPart),
                    },
                    new FooterReference
                    {
                        Type = HeaderFooterValues.Default,
                        Id = main.GetIdOfPart(footerPart),
                    }));
        }

        return stream.ToArray();
    }

    private static byte[] CreateTemplate(params OpenXmlElement[] elements)
    {
        using var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(new Body(elements));
        }

        return stream.ToArray();
    }

    private static Paragraph Paragraph(string text) =>
        new(new Run(new Text(text)));

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

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "akode-docxgen-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
