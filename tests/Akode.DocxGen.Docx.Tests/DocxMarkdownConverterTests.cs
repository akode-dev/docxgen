using System.Text;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Conversion;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Docx.Tests;

public sealed class DocxMarkdownConverterTests
{
    [Fact]
    public async Task InsertsNumberingBeforeMacCleanupInAStyleReference()
    {
        using var styleReference = CreateStyleReferenceWithNumberingCleanup();
        using var markdown = new MemoryStream(
            Encoding.UTF8.GetBytes(
                """
                - [x] Completed
                - [ ] Pending

                3. Third
                4. Fourth
                """),
            writable: false);

        var result = await new DocxMarkdownConverter().ConvertAsync(
            new ConvertRequest(
                new InputArtifact("tasks.md", markdown),
                new InputArtifact("style.docx", styleReference),
                validateOutput: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = result.Document;

        result.IsSuccess.ShouldBeTrue();
        result.Diagnostics.ShouldNotContain(
            diagnostic => diagnostic.Code == DiagnosticCode.MarkdownFeatureDowngraded);
        result.Validation.ShouldNotBeNull();
        result.Validation.IsValid.ShouldBeTrue();

        using var package = WordprocessingDocument.Open(document, false);
        var numbering = package.MainDocumentPart?
            .NumberingDefinitionsPart?
            .Numbering;
        numbering.ShouldNotBeNull();
        numbering!.ChildElements[^1].ShouldBeOfType<NumberingIdMacAtCleanup>();
        var listParagraphs = package.MainDocumentPart!.Document!.Body!
            .Elements<Paragraph>()
            .Where(paragraph =>
                paragraph.ParagraphProperties?.NumberingProperties is not null)
            .ToArray();
        listParagraphs.Length.ShouldBe(4);
        listParagraphs[0].InnerText.ShouldBe("Completed");
        listParagraphs[1].InnerText.ShouldBe("Pending");
        ResolveMarker(numbering, listParagraphs[0]).ShouldBe("☒");
        ResolveMarker(numbering, listParagraphs[1]).ShouldBe("☐");
        listParagraphs[2].InnerText.ShouldBe("Third");
        listParagraphs[3].InnerText.ShouldBe("Fourth");
        package.MainDocumentPart.Document.Body.InnerText.ShouldNotContain("☒");
        package.MainDocumentPart.Document.Body.InnerText.ShouldNotContain("☐");
    }

    [Fact]
    public async Task RendersNestedTaskCheckboxesAsTheOnlyListMarkers()
    {
        using var markdown = new MemoryStream(
            Encoding.UTF8.GetBytes(
                """
                - [x] Parent
                    - [ ] Child
                """),
            writable: false);

        var result = await new DocxMarkdownConverter().ConvertAsync(
            new ConvertRequest(
                new InputArtifact("nested-tasks.md", markdown),
                validateOutput: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = result.Document;

        result.IsSuccess.ShouldBeTrue();
        result.Validation.ShouldNotBeNull();
        result.Validation.IsValid.ShouldBeTrue();

        using var package = WordprocessingDocument.Open(document, false);
        var numbering = package.MainDocumentPart?
            .NumberingDefinitionsPart?
            .Numbering;
        numbering.ShouldNotBeNull();
        var listParagraphs = package.MainDocumentPart!.Document!.Body!
            .Elements<Paragraph>()
            .Where(paragraph =>
                paragraph.ParagraphProperties?.NumberingProperties is not null)
            .ToArray();
        listParagraphs.Length.ShouldBe(2);
        listParagraphs[0].InnerText.ShouldBe("Parent");
        listParagraphs[1].InnerText.ShouldBe("Child");
        ResolveMarker(numbering!, listParagraphs[0]).ShouldBe("☒");
        ResolveMarker(numbering, listParagraphs[1]).ShouldBe("☐");
        ResolveLevel(listParagraphs[0]).ShouldBe(0);
        ResolveLevel(listParagraphs[1]).ShouldBe(1);
        package.MainDocumentPart.Document.Body.InnerText.ShouldNotContain("☒");
        package.MainDocumentPart.Document.Body.InnerText.ShouldNotContain("☐");
    }

    [Fact]
    public async Task RendersOrderedTaskItemsWithCheckboxMarkersInsteadOfNumbers()
    {
        using var markdown = new MemoryStream(
            Encoding.UTF8.GetBytes(
                """
                1. [x] Completed
                2. [ ] Pending
                """),
            writable: false);

        var result = await new DocxMarkdownConverter().ConvertAsync(
            new ConvertRequest(
                new InputArtifact("ordered-tasks.md", markdown),
                validateOutput: true),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = result.Document;

        result.IsSuccess.ShouldBeTrue();
        result.Validation.ShouldNotBeNull();
        result.Validation.IsValid.ShouldBeTrue();

        using var package = WordprocessingDocument.Open(document, false);
        var numbering = package.MainDocumentPart?
            .NumberingDefinitionsPart?
            .Numbering;
        numbering.ShouldNotBeNull();
        var listParagraphs = package.MainDocumentPart!.Document!.Body!
            .Elements<Paragraph>()
            .Where(paragraph =>
                paragraph.ParagraphProperties?.NumberingProperties is not null)
            .ToArray();
        listParagraphs.Length.ShouldBe(2);
        listParagraphs[0].InnerText.ShouldBe("Completed");
        listParagraphs[1].InnerText.ShouldBe("Pending");
        ResolveMarker(numbering!, listParagraphs[0]).ShouldBe("☒");
        ResolveMarker(numbering, listParagraphs[1]).ShouldBe("☐");
        package.MainDocumentPart.Document.Body.InnerText.ShouldNotContain("☒");
        package.MainDocumentPart.Document.Body.InnerText.ShouldNotContain("☐");
    }

    private static string? ResolveMarker(
        Numbering numbering,
        Paragraph paragraph)
    {
        var properties = paragraph.ParagraphProperties?.NumberingProperties;
        var numberId = properties?.NumberingId?.Val?.Value;
        var level = properties?.NumberingLevelReference?.Val?.Value ?? 0;
        var abstractId = numbering
            .Elements<NumberingInstance>()
            .Single(instance => instance.NumberID?.Value == numberId)
            .AbstractNumId?
            .Val?
            .Value;
        return numbering
            .Elements<AbstractNum>()
            .Single(item => item.AbstractNumberId?.Value == abstractId)
            .Elements<Level>()
            .Single(item => item.LevelIndex?.Value == level)
            .LevelText?
            .Val?
            .Value;
    }

    private static int ResolveLevel(Paragraph paragraph) =>
        paragraph
            .ParagraphProperties?
            .NumberingProperties?
            .NumberingLevelReference?
            .Val?
            .Value ?? 0;

    private static MemoryStream CreateStyleReferenceWithNumberingCleanup()
    {
        var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text("Sample")))));
            var numberingPart = main.AddNewPart<NumberingDefinitionsPart>();
            numberingPart.Numbering = new Numbering(
                new AbstractNum(
                    new Level(
                        new StartNumberingValue { Val = 1 },
                        new NumberingFormat { Val = NumberFormatValues.Decimal },
                        new LevelText { Val = "%1." })
                    {
                        LevelIndex = 0,
                    })
                {
                    AbstractNumberId = 0,
                },
                new NumberingInstance(new AbstractNumId { Val = 0 })
                {
                    NumberID = 1,
                },
                new NumberingIdMacAtCleanup { Val = 1 });
            numberingPart.Numbering.Save();
            main.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }
}
