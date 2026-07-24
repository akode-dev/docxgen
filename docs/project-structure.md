# Project structure

This map describes the intended repository. Files already present form the
compileable scaffold; files marked “planned” are implementation targets.

```text
Akode.DocxGen.sln
AGENTS.md
CLAUDE.md
README.md
Directory.Build.props
Directory.Packages.props
global.json
nuget.config

src/
  Akode.DocxGen.Core/
    Abstractions/
    Diagnostics/
    Markdown/                 planned
    Model/
    Pipeline/
    Security/                 planned
  Akode.DocxGen.Docx/
    Formatters/
    Markdown/                 planned bounded block renderer
    PostProcessing/
  Akode.DocxGen.Cli/
    Commands/
    Output/
  Akode.DocxGen.Mcp/
    Tools/

tests/
  Akode.DocxGen.Core.Tests/
  Akode.DocxGen.Docx.Tests/
  Akode.DocxGen.Cli.Tests/
  TestAssets/

templates/
samples/
docs/
  adr/
  schemas/
eng/
  hooks/
.codex/
.claude/
.azuredevops/
.github/workflows/
```

## Root files

| Path | Responsibility |
|---|---|
| `README.md` | Human entry point and product overview |
| `AGENTS.md` | Durable Codex repository instructions |
| `CLAUDE.md` | Claude Code instructions importing shared rules |
| `Directory.Build.props` | Framework, language, analyzers, reproducibility |
| `Directory.Packages.props` | Every NuGet version |
| `global.json` | .NET SDK feature-band policy |
| `nuget.config` | Approved package sources |
| `.editorconfig` | Formatting and code-style policy |
| `.gitattributes` | Text/binary and line-ending behavior |
| `.gitignore` | Generated output, local settings, secrets |
| `CHANGELOG.md` | User-visible release history |
| `THIRD-PARTY-NOTICES.md` | Resolved package license inventory |

## `Akode.DocxGen.Core`

### `Abstractions`

- `ITemplateInspector`: converts a template stream into `TemplateSchema`.
- `IDocumentRenderer`: converts template plus `BoundModel` into a document
  stream.
- `IDocumentPostProcessor`: ordered deterministic output mutation.
- `IOoxmlValidator`: technology-neutral validation contract.
- `IAssetResolver`: local path-controlled asset loading.

### `Model`

Present:

- `TemplateSchema` and `TemplatePlaceholder`;
- `BoundModel` and `MarkdownStats`;
- asset and validation result contracts;
- `ModelValueKind`.

Planned:

- `ModelJsonReader`;
- `ModelSchemaValidator`;
- directive converters for `$md`, `$mdFile`, `$file`, `$text`;
- template identity and contract metadata;
- document-output naming model.

### `Markdown` (planned)

- `MarkdownPipelineFactory`;
- `MarkdownPreprocessor`;
- `SectionAnchorParser`;
- `MarkdownTableToCollection`;
- image-reference resolver;
- unsupported-feature downgrade visitor.

### `Pipeline`

Present options and outcomes will be extended with:

- `RenderRequest`;
- `RenderResult`;
- `ValidateModelRequest`;
- `RenderPipeline`;
- source merge/reconciliation services;
- output-name resolution.

### `Diagnostics`

Contains stable process exit codes and the diagnostic registry. A completeness
test must ensure every registered error has default message and hint metadata.

### `Security` (planned)

- `PathGuard`;
- file/aggregate size limits;
- ZIP safety policy;
- URI policy;
- safe media-type detection.

## `Akode.DocxGen.Docx`

Planned implementation files:

- `DocxTemplaterRenderer`;
- `DocxTemplaterInspector`;
- `MarkdownSlotFormatter`;
- Markdown block renderers for paragraphs, headings, lists, tables, quotes,
  code, links, and inline images;
- `OpenXmlValidatorAdapter`;
- formatter registration;
- `UpdateFieldsPostProcessor`;
- `DocumentPropertiesPostProcessor`;
- `TableGeometryPostProcessor`;
- `ImageGeometryPostProcessor`;
- `EmptyParagraphCleanup`;
- `LeftoverPlaceholderScanner`;
- `DocxNormalizer` for tests.

This project is the only production project allowed to depend on
DocxTemplater or Open XML SDK. It must not reference the rejected
`DocxTemplater.Markdown` or `DocxTemplater.Images` extensions.

## `Akode.DocxGen.Cli`

Planned command pairs:

```text
Commands/
  InspectCommand.cs
  InspectCommandHandler.cs
  ScaffoldModelCommand.cs
  ScaffoldModelCommandHandler.cs
  ValidateModelCommand.cs
  ValidateModelCommandHandler.cs
  RenderCommand.cs
  RenderCommandHandler.cs
  ConvertCommand.cs
  ConvertCommandHandler.cs
  ValidateCommand.cs
  ValidateCommandHandler.cs
```

`Output` contains JSON and human writers. No other project writes to
`Console`.

## `Akode.DocxGen.Mcp`

Phase 2 only. Tool methods convert MCP DTOs to Core requests and return Core
results. The MCP package/version is not pinned until implementation begins.

## Tests

### Core tests

Fast unit and property tests. Avoid disk I/O where streams and in-memory models
are sufficient.

### Docx tests

Integration, package structure, normalized golden outputs, style behavior, and
rendered visual fixtures.

### CLI tests

Command parsing, stdout/stderr separation, JSON schemas, exit codes, atomic
write behavior, and process-level scenarios.

### TestAssets

Synthetic templates, Markdown, models, images, and normalized golden output.
Customer material is prohibited.

## Templates and samples

`templates/` holds reference template packages only after branding has been
approved and anonymized. Every template has an adjacent schema.

`samples/` holds a complete non-confidential authoring example and scripts that
will become executable once Phase 1 CLI commands are implemented.

## Agent configuration

- `.codex/config.toml`: minimal trusted-project root detection; durable behavior
  remains in `AGENTS.md`.
- `.claude/settings.json`: shared command permissions and explicit destructive
  command denials.
- `eng/hooks/`: hook entrypoints that call `validate-model`; activation waits
  until that command is implemented and tested.

## CI

`.azuredevops/azure-pipelines.yml` is the primary build definition.
`.github/workflows/ci.yml` provides equivalent GitHub validation for the
current remote.
