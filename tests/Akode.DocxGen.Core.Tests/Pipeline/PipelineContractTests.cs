using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Pipeline;

public sealed class PipelineContractTests
{
    [Fact]
    public void InputArtifactRequiresAReadableStream()
    {
        var disposed = new MemoryStream();
        disposed.Dispose();

        Should.Throw<ArgumentException>(() => new InputArtifact("model.json", disposed));
        Should.Throw<ArgumentException>(() => new InputArtifact(" ", Stream.Null));
    }

    [Fact]
    public void RenderRequestRequiresModelOrMarkdown()
    {
        var template = new InputArtifact("template.docx", Stream.Null);

        Should.Throw<ArgumentException>(
            () => new RenderRequest(
                template,
                model: null,
                markdown: null,
                assetsRoot: ".",
                new RenderOptions
                {
                    Culture = "en-US",
                    Strict = true,
                }));
    }

    [Fact]
    public void RenderRequestSnapshotsMutableDictionaries()
    {
        var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ds.Document.Version"] = "1.0",
        };
        var request = new RenderRequest(
            new InputArtifact("template.docx", Stream.Null),
            new InputArtifact("model.json", Stream.Null),
            markdown: null,
            assetsRoot: ".",
            new RenderOptions
            {
                Culture = "en-US",
                Strict = true,
            },
            overrides);

        overrides["ds.Document.Version"] = "2.0";

        request.Overrides["ds.Document.Version"].ShouldBe("1.0");
    }

    [Fact]
    public void ModelAndRenderResultsDeriveSuccessFromDiagnostics()
    {
        var error = DiagnosticRegistry.Create(DiagnosticCode.ModelSchemaViolation);
        var validModel = new BoundModel(
            new Dictionary<string, object?>(StringComparer.Ordinal),
            [],
            [],
            MarkdownStats.Empty);

        new ValidateModelResult("template", "model", validModel, []).IsValid
            .ShouldBeTrue();
        new ValidateModelResult("template", "model", validModel, [error]).IsValid
            .ShouldBeFalse();
        new RenderResult(
            Document: null,
            TemplateHash: "template",
            ModelHash: "model",
            BoundPaths: [],
            UnboundPaths: [],
            MarkdownStats.Empty,
            Validation: null,
            Diagnostics: [error],
            DryRun: true).IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateDocumentRequestRequiresPositiveErrorLimit(int maxErrors)
    {
        var document = new InputArtifact("output.docx", Stream.Null);

        Should.Throw<ArgumentOutOfRangeException>(
            () => new ValidateDocumentRequest(document, maxErrors));
    }

    [Theory]
    [InlineData(-6)]
    [InlineData(6)]
    public void ConvertRequestEnforcesSchemaHeadingRange(int headingOffset)
    {
        var markdown = new InputArtifact("proposal.md", Stream.Null);

        Should.Throw<ArgumentOutOfRangeException>(
            () => new ConvertRequest(markdown, headingOffset: headingOffset));
    }

    [Fact]
    public void ExtractRequestNormalizesMarkdownImagePrefix()
    {
        var request = new ExtractRequest(
            new InputArtifact("document.docx", Stream.Null),
            @"document.assets\images\");

        request.ImagePathPrefix.ShouldBe("document.assets/images");
        Should.Throw<ArgumentException>(
            () => new ExtractRequest(
                new InputArtifact("document.docx", Stream.Null),
                " "));
    }

    [Fact]
    public void ExtractedAssetSnapshotsBytesAndRejectsDirectories()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var asset = new ExtractedAsset("image-001.png", "image/png", bytes);

        bytes[0] = 9;

        asset.Content.ToArray().ShouldBe([1, 2, 3]);
        Should.Throw<ArgumentException>(
            () => new ExtractedAsset(
                "nested/image.png",
                "image/png",
                bytes));
    }

    [Theory]
    [InlineData(" template", null)]
    [InlineData("template/id", null)]
    [InlineData("шаблон", null)]
    [InlineData("template", "1.0+build")]
    [InlineData("template", " ")]
    public void GenerateSchemaRequestRejectsUnsafeIdentityParts(
        string templateId,
        string? templateVersion)
    {
        var template = new InputArtifact("template.docx", Stream.Null);

        Should.Throw<ArgumentException>(
            () => new GenerateSchemaRequest(
                template,
                templateId,
                templateVersion));
    }

    [Fact]
    public void GenerateSchemaResultDerivesSuccessFromDiagnostics()
    {
        var error = DiagnosticRegistry.Create(DiagnosticCode.SchemaNoBindings);

        new GenerateSchemaResult(
            "{}",
            "template",
            "1.0.0",
            "sha256:hash",
            1,
            []).IsSuccess.ShouldBeTrue();
        new GenerateSchemaResult(
            string.Empty,
            "template",
            "1.0.0",
            "sha256:hash",
            0,
            [error]).IsSuccess.ShouldBeFalse();
    }
}
