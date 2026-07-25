# Embedding DocxGen in a .NET application

Use the `Akode.DocxGen` package when the document pipeline should run in
process instead of through the CLI:

```shell
dotnet add package Akode.DocxGen
```

`DocxGenPipelineFactory` composes the supported template inspector, renderer,
Markdown converter, DOCX extractor, post-processors, and Open XML validator.

## Render a document

```csharp
using Akode.DocxGen;
using Akode.DocxGen.Core.Pipeline;

var pipeline = DocxGenPipelineFactory.CreatePipeline();

await using var template = File.OpenRead("templates/proposal.docx");
await using var model = File.OpenRead("model.json");

var result = await pipeline.RenderAsync(
    new RenderRequest(
        template: new InputArtifact("proposal.docx", template),
        model: new InputArtifact("model.json", model),
        markdown: null,
        assetsRoot: Path.GetFullPath("assets"),
        options: new RenderOptions
        {
            Culture = "en-US",
            Strict = true,
            AllowRawHtml = false,
            AllowRemoteImages = false,
            UpdateFieldsOnOpen = true,
        },
        validateOutput: true));

if (!result.IsSuccess || result.Document is null)
{
    var messages = string.Join(
        Environment.NewLine,
        result.Diagnostics.Select(
            diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
    throw new InvalidOperationException(messages);
}

await using (result.Document)
await using (var output = File.Create("Proposal.docx"))
{
    await result.Document.CopyToAsync(output);
}
```

Input stream lifetime belongs to the caller. The returned document stream
belongs to the caller and must be disposed.

## Inspect before collecting data

```csharp
await using var template = File.OpenRead("templates/proposal.docx");
var inspection = await pipeline.InspectAsync(
    new InspectRequest(
        new InputArtifact("proposal.docx", template)));

foreach (var placeholder in inspection.Schema.Placeholders)
{
    Console.WriteLine($"{placeholder.Path}: {placeholder.Kind}");
}
```

The same pipeline also exposes `GenerateSchemaAsync`, `ScaffoldModelAsync`,
`ValidateModelAsync`, `ConvertAsync`, `ExtractAsync`, and
`ValidateDocument`.

## Dependency injection

Applications may register the factory result as a singleton when their host
owns the pipeline lifetime:

```csharp
services.AddSingleton(_ => DocxGenPipelineFactory.CreatePipeline());
```

The default implementation is stateless; input/output streams remain scoped
to each request. Hosts that need custom adapters can construct
`Akode.DocxGen.Core.Pipeline.DocxGenPipeline` directly from the Core
interfaces.

## Choosing a package

| Package | Choose it when |
|---|---|
| `Akode.DocxGen` | The application needs the ready-to-use DOCX implementation |
| `Akode.DocxGen.Core` | The application supplies adapters or only consumes contracts |
| `Akode.DocxGen.Tool` | A process or agent should call the `docxgen` executable |

Public requests, results, diagnostics, and report DTOs are treated as API.
Review `CHANGELOG.md` before upgrading and pin a package version in production.
