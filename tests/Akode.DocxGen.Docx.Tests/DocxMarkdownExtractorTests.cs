using System.Text;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Conversion;
using Akode.DocxGen.Docx.Extraction;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Docx.Tests;

public sealed class DocxMarkdownExtractorTests
{
    private static readonly byte[] PixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task RoundTripPreservesSupportedSemanticBlocksAndImage()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Akode.DocxGen.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var markdownPath = Path.Combine(root, "source.md");
            await File.WriteAllBytesAsync(
                Path.Combine(root, "pixel.png"),
                PixelPng,
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            const string markdown =
                """
                # Architecture

                Paragraph with **bold**, *italic*, `code`, and [link](https://example.com).

                - Bullet first
                - Bullet second

                1. First
                2. Second

                > A quoted sentence.

                ```text
                alpha
                beta
                ```

                | Name | Value |
                | --- | --- |
                | Alpha | Beta |

                ---

                ![pixel](pixel.png)
                """;
            using var input = new MemoryStream(
                Encoding.UTF8.GetBytes(markdown),
                writable: false);
            var converted = await new DocxMarkdownConverter().ConvertAsync(
                new ConvertRequest(
                    new InputArtifact(markdownPath, input)),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            using var document = converted.Document;

            var result = await new DocxMarkdownExtractor().ExtractAsync(
                new ExtractRequest(
                    new InputArtifact("roundtrip.docx", document),
                    "roundtrip.assets"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            result.IsSuccess.ShouldBeTrue();
            result.Markdown.ShouldContain("# Architecture");
            result.Markdown.ShouldContain("**bold**");
            result.Markdown.ShouldContain("*italic*");
            result.Markdown.ShouldContain("`code`");
            result.Markdown.ShouldContain("[link](https://example.com)");
            result.Markdown.ShouldContain("- Bullet first");
            result.Markdown.ShouldContain("- Bullet second");
            result.Markdown.ShouldContain("1. First");
            result.Markdown.ShouldContain("2. Second");
            result.Markdown.ShouldContain("> A quoted sentence.");
            result.Markdown.ShouldContain("alpha");
            result.Markdown.ShouldContain("```");
            result.Markdown.ShouldContain("Name");
            result.Markdown.ShouldContain("Value");
            result.Markdown.ShouldContain("| --- | --- |");
            result.Markdown.ShouldContain("\n---\n");
            result.Markdown.ShouldContain(
                "![pixel](roundtrip.assets/image-001.png)");
            result.Assets.ShouldHaveSingleItem();
            result.Assets[0].Content.ToArray().ShouldBe(PixelPng);
            result.Stats.Headings.ShouldBe(1);
            result.Stats.ListItems.ShouldBe(4);
            result.Stats.Tables.ShouldBe(1);
            result.Stats.Images.ShouldBe(1);
            result.Diagnostics.ShouldNotContain(
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

            var extractedAssetsRoot = Path.Combine(root, "roundtrip.assets");
            Directory.CreateDirectory(extractedAssetsRoot);
            foreach (var asset in result.Assets)
            {
                await File.WriteAllBytesAsync(
                    Path.Combine(extractedAssetsRoot, asset.FileName),
                    asset.Content.ToArray(),
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
            }

            using var extractedMarkdown = new MemoryStream(
                Encoding.UTF8.GetBytes(result.Markdown),
                writable: false);
            var reconverted = await new DocxMarkdownConverter().ConvertAsync(
                new ConvertRequest(
                    new InputArtifact(
                        Path.Combine(root, "roundtrip.md"),
                        extractedMarkdown)),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            using var reconvertedDocument = reconverted.Document;
            reconverted.IsSuccess.ShouldBeTrue();
            reconvertedDocument.Length.ShouldBeGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InvalidPackageReturnsStableExtractionDiagnostic()
    {
        using var input = new MemoryStream(
            Encoding.UTF8.GetBytes("not a DOCX"),
            writable: false);

        var result = await new DocxMarkdownExtractor().ExtractAsync(
            new ExtractRequest(
                new InputArtifact("broken.docx", input),
                "broken.assets"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeFalse();
        result.Markdown.ShouldBeEmpty();
        result.Assets.ShouldBeEmpty();
        result.Diagnostics.ShouldHaveSingleItem();
        result.Diagnostics[0].Code.ShouldBe(DiagnosticCode.ExtractionFailure);
        result.Diagnostics[0].Hint.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GeneratedTocFieldIsOmittedWithWarning()
    {
        using var input = new MemoryStream(
            Encoding.UTF8.GetBytes("# Chapter"),
            writable: false);
        var converted = await new DocxMarkdownConverter().ConvertAsync(
            new ConvertRequest(
                new InputArtifact("source.md", input),
                includeToc: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = converted.Document;

        var result = await new DocxMarkdownExtractor().ExtractAsync(
            new ExtractRequest(
                new InputArtifact("toc.docx", document),
                "toc.assets"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Markdown.ShouldContain("# Chapter");
        result.Markdown.ShouldNotContain("TOC \\", Case.Insensitive);
        result.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.ExtractionFieldOmitted);
    }

    [Fact]
    public async Task DuplicateStyleIdentifiersDoNotPreventHeadingExtraction()
    {
        using var packageStream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   packageStream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(
                new Body(
                    new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId { Val = "Heading1" }),
                        new Run(new Text("Top section"))),
                    new Paragraph(
                        new ParagraphProperties(
                            new ParagraphStyleId { Val = "CustomSeven" }),
                        new Run(new Text("Deep section")))));

            var stylePart = main.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = new Styles(
                CreateParagraphStyle("Heading1", "Heading 1", outlineLevel: 0),
                CreateParagraphStyle("Heading1", "Heading 1", outlineLevel: 0),
                CreateParagraphStyle("CustomSeven", "Heading-7", outlineLevel: 6));
        }

        using var input = new MemoryStream(
            packageStream.ToArray(),
            writable: false);
        var result = await new DocxMarkdownExtractor().ExtractAsync(
            new ExtractRequest(
                new InputArtifact("duplicate-styles.docx", input),
                "duplicate-styles.assets"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Markdown.ShouldContain("# Top section");
        result.Markdown.ShouldContain("###### Deep section");
        result.Stats.Headings.ShouldBe(2);
        result.Diagnostics.Count(diagnostic =>
            diagnostic.Code == DiagnosticCode.ExtractionFeatureDowngraded)
            .ShouldBe(2);
        result.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Message.Contains(
                "Duplicate Word style identifier",
                StringComparison.Ordinal));
        result.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Message.Contains(
                "Word heading level 7",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task DuplicateNumberingIdentifiersDoNotPreventListExtraction()
    {
        using var packageStream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   packageStream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(
                new Body(
                    new Paragraph(
                        new ParagraphProperties(
                            new NumberingProperties(
                                new NumberingLevelReference { Val = 0 },
                                new NumberingId { Val = 1 })),
                        new Run(new Text("First item")))));

            var numberingPart = main.AddNewPart<NumberingDefinitionsPart>();
            numberingPart.Numbering = new Numbering(
                CreateAbstractNumber(1, NumberFormatValues.Bullet),
                CreateAbstractNumber(1, NumberFormatValues.Decimal),
                new NumberingInstance(new AbstractNumId { Val = 1 })
                {
                    NumberID = 1,
                },
                new NumberingInstance(new AbstractNumId { Val = 1 })
                {
                    NumberID = 1,
                });
        }

        using var input = new MemoryStream(
            packageStream.ToArray(),
            writable: false);
        var result = await new DocxMarkdownExtractor().ExtractAsync(
            new ExtractRequest(
                new InputArtifact("duplicate-numbering.docx", input),
                "duplicate-numbering.assets"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Markdown.ShouldContain("- First item");
        result.Stats.ListItems.ShouldBe(1);
        result.Diagnostics.Count(diagnostic =>
            diagnostic.Code == DiagnosticCode.ExtractionFeatureDowngraded)
            .ShouldBe(2);
        result.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Message.Contains(
                "Duplicate Word numbering identifier",
                StringComparison.Ordinal));
        result.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Message.Contains(
                "Duplicate Word abstract numbering identifier",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task FieldIsOmittedWithoutDroppingSurroundingParagraphText()
    {
        using var packageStream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   packageStream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(
                new Body(
                    new Paragraph(
                        new Run(new Text("Before ")),
                        new Run(new FieldChar
                        {
                            FieldCharType = FieldCharValues.Begin,
                        }),
                        new Run(new FieldCode(" DATE ")),
                        new Run(new FieldChar
                        {
                            FieldCharType = FieldCharValues.Separate,
                        }),
                        new Run(new Text("July 25, 2026")),
                        new Run(new FieldChar
                        {
                            FieldCharType = FieldCharValues.End,
                        }),
                        new Run(new Text(" after")))));
        }

        using var input = new MemoryStream(
            packageStream.ToArray(),
            writable: false);
        var result = await new DocxMarkdownExtractor().ExtractAsync(
            new ExtractRequest(
                new InputArtifact("field.docx", input),
                "field.assets"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        result.Markdown.ShouldContain("Before");
        result.Markdown.ShouldContain("after");
        result.Markdown.ShouldNotContain("July 25, 2026");
        result.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.ExtractionFieldOmitted);
    }

    [Fact]
    public async Task MacroEnabledPackageIsRejected()
    {
        using var packageStream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   packageStream,
                   WordprocessingDocumentType.MacroEnabledDocument,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(
                new Body(new Paragraph(new Run(new Text("Unsafe")))));
        }

        using var input = new MemoryStream(
            packageStream.ToArray(),
            writable: false);
        var result = await new DocxMarkdownExtractor().ExtractAsync(
            new ExtractRequest(
                new InputArtifact("unsafe.docm", input),
                "unsafe.assets"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeFalse();
        result.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.ExtractionFailure);
        result.Diagnostics[0].Message.ShouldContain("Macro-enabled");
    }

    private static Style CreateParagraphStyle(
        string id,
        string name,
        int outlineLevel) =>
        new(
            new StyleName { Val = name },
            new StyleParagraphProperties(
                new OutlineLevel { Val = outlineLevel }))
        {
            Type = StyleValues.Paragraph,
            StyleId = id,
        };

    private static AbstractNum CreateAbstractNumber(
        int id,
        NumberFormatValues format) =>
        new(
            new Level(
                new StartNumberingValue { Val = 1 },
                new NumberingFormat { Val = format })
            {
                LevelIndex = 0,
            })
        {
            AbstractNumberId = id,
        };
}
