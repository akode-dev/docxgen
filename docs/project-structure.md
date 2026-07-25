# Project structure

```text
DocxGen.sln
AGENTS.md
CLAUDE.md
README.md
CONTRIBUTING.md
CODE_OF_CONDUCT.md
SECURITY.md
SUPPORT.md
CHANGELOG.md
THIRD-PARTY-NOTICES.md
Directory.Build.props
Directory.Packages.props
global.json
nuget.config

src/
  Akode.DocxGen.Core/
    Abstractions/       technology-neutral adapter boundaries
    Diagnostics/        stable codes, registry, exit codes
    Markdown/           anchors, GFM tables, neutral Markdown AST/parser
    Model/              JSON reader, values, merge, binding, schema generation
    Pipeline/           requests/results and DocxGenPipeline orchestration
    Reports/            versioned command-report DTOs
    Security/           containment, limits, local/remote asset resolution
  Akode.DocxGen.Docx/
    Conversion/         standalone Markdown-to-DOCX
    Extraction/         semantic DOCX-to-Markdown and embedded assets
    Inspection/         package-wide template inspection
    PostProcessing/     fields, properties, leftover markers
    Rendering/          DocxTemplater binding and Open XML Markdown rendering
    Utilities/          stream ownership helpers
    Validation/         Open XML SDK validation
    DocxGenPipelineFactory.cs
                        ready-to-use public application facade
  Akode.DocxGen.Cli/
    Commands/           System.CommandLine command factory/handlers
    Output/             JSON/human output, atomic files, versioned names
    Properties/         test visibility metadata
    CompositionRoot.cs  dependency registration
    Program.cs           thin executable host
  Akode.DocxGen.Mcp/     Phase 2 placeholder only

tests/
  Akode.DocxGen.Core.Tests/
  Akode.DocxGen.Docx.Tests/
  Akode.DocxGen.Cli.Tests/
  TestAssets/

templates/
  proposal.docx
  proposal.schema.json
samples/
  model.json
  proposal.md
  architecture.svg
docs/
  adr/
  schemas/
  spikes/
  embedding.md
eng/
  hooks/
  verify.ps1
  verify.sh
  generate-third-party-notices.ps1
  package-license-allowlist.json
research/
  renderer-spike/       isolated historical experiment, not production
.github/
  ISSUE_TEMPLATE/
  workflows/
    ci.yml
    release.yml
```

## Responsibilities and boundaries

### Core

Core contains no Open XML or CLI implementation types. It owns:

- model/schema reading and adjacent-template reconciliation;
- deterministic structural schema generation from neutral template shapes;
- deterministic Markdown preprocessing and source merging;
- local/remote asset security policy;
- strict/lenient binding;
- adapter contracts and pipeline orchestration;
- diagnostics and report DTOs.

### DOCX adapter

The adapter is the only production project that references DocxTemplater and
Open XML SDK. It owns template discovery, binding, native Word elements,
post-processing, forward conversion, semantic extraction, validation, and the
ready-to-use `Akode.DocxGen` facade. The rejected `DocxTemplater.Markdown`
dependency remains only in the retained research experiment.

### CLI

The CLI translates options and streams into Core requests, maps results to
stable reports/exit codes, and writes output atomically. Business/rendering
logic does not belong in command handlers. Console access is confined to
`Output`.

### MCP

MCP is deferred to Phase 2. It must adapt Core requests/results and must not
shell out to the CLI or duplicate validation/rendering.

## Templates and samples

`templates/` contains synthetic or approved governed templates. Each production
template has an adjacent `.schema.json` with exact ID, version, hash, fields,
and collections. The structural contract can be regenerated with
`generate-schema`; `samples/` is a complete non-confidential runnable example.

## Tests and gates

- Core tests cover schemas, parsing, merge, security, diagnostics, reports,
  architecture, and license inventory.
- DOCX tests run the real reference render and inspect the generated package.
- CLI tests cover naming and host behavior.
- visual QA follows `docs/template-authoring-guide.md`;
- every resolved NuGet package/version must match the reviewed allow-list.

## Generated output

Builds, packages, published binaries, rendered samples, and QA page images go
under ignored `artifacts/`, `bin/`, and `obj/` directories.

## Published surfaces

| Repository project | Published surface |
|---|---|
| `Akode.DocxGen.Core` | `Akode.DocxGen.Core` NuGet contracts package |
| `Akode.DocxGen.Docx` | `Akode.DocxGen` ready-to-use NuGet package |
| `Akode.DocxGen.Cli` | `Akode.DocxGen.Tool` and `docxgen` executables |
| `Akode.DocxGen.Mcp` | Unpublished Phase 2 placeholder |
