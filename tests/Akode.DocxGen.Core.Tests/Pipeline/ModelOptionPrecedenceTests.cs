using System.Text;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Pipeline;

public sealed class ModelOptionPrecedenceTests
{
    [Fact]
    public async Task UsesModelDefaultsExceptForExplicitCallerOverrides()
    {
        const string modelJson =
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "fake", "version": "1.0.0" },
              "options": {
                "culture": "pl-PL",
                "strict": false,
                "headingOffset": 2,
                "allowRawHtml": true,
                "allowRemoteImages": false,
                "updateFieldsOnOpen": false
              },
              "data": {
                "ds": {
                  "Body": { "$md": "# Heading" }
                }
              }
            }
            """;
        var renderer = new CapturingRenderer();
        var pipeline = new DocxGenPipeline(
            new FakeInspector(),
            renderer,
            new UnusedConverter(),
            new UnusedValidator(),
            []);
        using var template = new MemoryStream([1, 2, 3], writable: false);
        using var model = new MemoryStream(
            Encoding.UTF8.GetBytes(modelJson),
            writable: false);

        var result = await pipeline.RenderAsync(
            new RenderRequest(
                new InputArtifact("fake.docx", template),
                new InputArtifact("model.json", model),
                markdown: null,
                RepositoryLayout.Root,
                new RenderOptions
                {
                    Culture = "en-US",
                    Strict = true,
                    HeadingOffset = 1,
                    Overrides = RenderOptionOverrides.HeadingOffset,
                }),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsSuccess.ShouldBeTrue();
        var options = renderer.Options.ShouldNotBeNull();
        options.Culture.ShouldBe("pl-PL");
        options.Strict.ShouldBeFalse();
        options.HeadingOffset.ShouldBe(1);
        options.AllowRawHtml.ShouldBeTrue();
        options.UpdateFieldsOnOpen.ShouldBeFalse();
        result.MarkdownStats.Headings.ShouldBe(1);
        result.UnboundPaths.ShouldContain("ds.Optional");
        result.Diagnostics.ShouldContain(
            diagnostic =>
                diagnostic.Code == DiagnosticCode.OptionalPlaceholderUnbound);
        await result.Document.ShouldNotBeNull()
            .DisposeAsync()
            .ConfigureAwait(true);
    }

    private sealed class FakeInspector : ITemplateInspector
    {
        public TemplateSchema Inspect(
            Stream templateDocument,
            CancellationToken cancellationToken = default) =>
            new(
                "fake",
                "1.0.0",
                $"sha256:{new string('0', 64)}",
                [
                    new TemplatePlaceholder(
                        "ds.Body",
                        ModelValueKind.Markdown,
                        "MD",
                        null,
                        Required: true,
                        ["main"],
                        [],
                        null),
                    new TemplatePlaceholder(
                        "ds.Optional",
                        ModelValueKind.Text,
                        null,
                        null,
                        Required: true,
                        ["main"],
                        [],
                        null),
                ],
                ["Normal", "Heading1"],
                []);
    }

    private sealed class CapturingRenderer : IDocumentRenderer
    {
        public RenderOptions? Options { get; private set; }

        public Task<RenderOutcome> RenderAsync(
            Stream templateDocument,
            BoundModel model,
            RenderOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(
                new RenderOutcome(
                    new MemoryStream([1], writable: true),
                    model.BoundPaths,
                    model.UnboundPaths,
                    model.MarkdownStats,
                    []));
        }
    }

    private sealed class UnusedConverter : IMarkdownDocumentConverter
    {
        public Task<ConvertResult> ConvertAsync(
            ConvertRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedValidator : IOoxmlValidator
    {
        public ValidationReport Validate(Stream document, int maxErrors = 50) =>
            throw new NotSupportedException();
    }
}
