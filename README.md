# Akode.DocxGen

`Akode.DocxGen` is a planned cross-platform .NET CLI for deterministic
generation of polished Word documents from:

1. a versioned DOCX template;
2. a validated JSON model;
3. one Markdown body or a set of anchored Markdown sections.

The target workflow is:

```text
(template.docx + model.json + proposal.md) -> docxgen -> proposal.docx
```

> Project status: repository scaffold and technical contract. The solution
> builds, but rendering commands are not implemented yet. Development must
> begin with the P0 rendering-engine spike described in
> [the implementation plan](docs/implementation-plan.md).

## Why this project exists

The document content should remain easy for people and AI agents to edit,
review, diff, and version. Markdown and JSON are good at that. Word remains the
right place for corporate branding, page sections, cover design, headers,
footers, a table of contents, and the final branded page.

The invariant is:

> Content lives in Markdown and JSON. Presentation lives in the DOCX template.

The tool must replace one-off Python/Node document scripts with a stable,
auditable command that works the same way locally, in CI, in Codex, and in
Claude Code.

## Document composition

A normal proposal template is expected to contain:

1. a cover page with scalar placeholders;
2. an optional Document Control page;
3. a real Word TOC field;
4. a standalone `{{ds.Body}:MD}` paragraph;
5. a section break and final branded page.

The body marker expands to any number of headings, paragraphs, lists, tables,
links, and figures. The final page moves automatically and remains last.

Example cover placeholders:

```text
{{ds.Document.Title}}
{{ds.Document.Description}}
{{ds.Document.Author.FirstName}} {{ds.Document.Author.LastName}}
{{ds.Document.ClientName}}
{{ds.Document.Date}}
{{ds.Document.Version}}
```

A fixed Akode logo belongs in the DOCX template. An image placeholder is
needed only for a variable asset, such as a client logo:

```text
{{ds.ClientLogo}:img}
```

## `inspect` versus `render`

`inspect` reads a template and returns its contract. It does not create a
document:

```powershell
docxgen inspect --template templates/proposal.docx --json
```

The result describes template identity and hash, placeholders, kinds, required
styles, locations, and warnings such as split Word runs.

`render` executes the document pipeline:

```powershell
docxgen render `
  --template templates/proposal.docx `
  --model model.json `
  --assets-dir . `
  --heading-offset 0 `
  --out out/Proposal.docx `
  --validate `
  --json
```

Rendering loads and validates the model, resolves Markdown and local assets,
reconciles them with the template contract, creates DOCX content, applies
post-processors, optionally validates OOXML, and writes the output atomically.

All commands above describe the planned public contract. They become supported
only when the corresponding acceptance tests are green.

## Model and schema

The JSON model separates document metadata from long-form content:

```json
{
  "$schema": "./docs/schemas/docxgen-model-1.0.schema.json",
  "modelVersion": "1.0",
  "template": {
    "id": "akode-proposal",
    "version": "1.0.0"
  },
  "options": {
    "culture": "en-US",
    "strict": true,
    "headingOffset": 0
  },
  "data": {
    "ds": {
      "Document": {
        "Title": "Customer Platform Proposal",
        "Description": "Technical and commercial proposal",
        "ClientName": "Example Corporation",
        "Date": "2026-07-24",
        "Version": "1.0",
        "Status": "Draft",
        "Author": {
          "FirstName": "Andrei",
          "LastName": "Ivanov"
        }
      },
      "Body": {
        "$mdFile": "proposal.md"
      }
    }
  }
}
```

Two validation layers are required:

- the base DocxGen schema validates the envelope and directives such as
  `$mdFile`, `$file`, `$md`, and `$text`;
- a template-specific schema validates required fields, types, constraints,
  and collections for one template.

The planned hook-friendly command is:

```powershell
docxgen validate-model `
  --template templates/proposal.docx `
  --model model.json `
  --assets-dir . `
  --json
```

It must return stable JSON paths, diagnostic codes, and actionable hints.
`render --dry-run` performs the complete preflight without writing a DOCX.

See [the model format](docs/model-format.md) and
[agent integration](docs/agent-integration.md).

## Versioned output names

Document version, template version, schema version, and tool version are
separate values. Only the document version belongs in the generated filename.

The planned convenience flag is:

```powershell
docxgen render `
  --template templates/proposal.docx `
  --model model.json `
  --out out/Proposal.docx `
  --append-document-version
```

For `data.ds.Document.Version = "3.0"`, the resolved output is:

```text
out/Proposal-v3.0.docx
```

The tool never auto-increments a version. It normalizes a leading `v`,
sanitizes invalid filename characters, refuses collisions unless `--overwrite`
is present, and returns the final path in its JSON report.

## Solution structure

```text
Akode.DocxGen.sln
src/
  Akode.DocxGen.Core/   contracts, model, Markdown, diagnostics, security
  Akode.DocxGen.Docx/   DocxTemplater and Open XML adapter
  Akode.DocxGen.Cli/    command-line host
  Akode.DocxGen.Mcp/    Phase 2 MCP adapter
tests/
  Akode.DocxGen.Core.Tests/
  Akode.DocxGen.Docx.Tests/
  Akode.DocxGen.Cli.Tests/
  TestAssets/
docs/
samples/
templates/
eng/
```

The complete file-by-file map is in
[project structure](docs/project-structure.md). Architectural boundaries are
in [architecture](docs/architecture.md), and the complete requirements are in
[the technical specification](docs/technical-specification.md).

## Build the scaffold

Prerequisites:

- .NET SDK 10.0.100 or a compatible 10.0 feature band;
- Git;
- Microsoft Word 2021/365 for final template acceptance;
- LibreOffice only for optional headless visual regression checks.

Windows:

```powershell
./eng/verify.ps1
```

Linux/macOS:

```bash
./eng/verify.sh
```

Equivalent commands:

```powershell
dotnet restore Akode.DocxGen.sln
dotnet build Akode.DocxGen.sln --configuration Release --no-restore
dotnet test Akode.DocxGen.sln --configuration Release --no-build
```

## Working with coding agents

Codex uses the root [`AGENTS.md`](AGENTS.md). Claude Code uses
[`CLAUDE.md`](CLAUDE.md), which imports the shared rules and agent guide.

Codex CLI:

```powershell
codex
```

Claude Code:

```powershell
claude
```

For hosted execution, configure the repository environment in the selected
service and use the same restore/build/test commands. Do not commit credentials
or provider-specific tokens. See [Codex and cloud execution](docs/codex-cloud.md)
and [the agent guide](docs/agent-guide.md).

## Branches

- `main`: stable, protected, releasable state;
- `develop`: integration for accepted feature branches;
- `feature/<name>` and `fix/<name>`: short-lived branches created from
  `develop`;
- `release/<version>` and `hotfix/<name>`: added only when releases begin.

Pull requests target `develop` during implementation. Release pull requests
merge `develop` into `main`, tag the released commit, and merge any release
fixes back to `develop`.

See [branching and releases](docs/branching-and-release.md).

## Delivery order

The first implementation is deliberately a risk-reduction spike:

1. prove Markdown rendering against a real branded template;
2. verify template styles, numbering, images, TOC, headers, footers, and text
   boxes;
3. decide whether DocxTemplater remains the rendering engine;
4. only then implement the production Core, adapters, and CLI.

Do not begin by writing a custom Markdown-to-OOXML renderer.

## Documentation

- [Technical specification](docs/technical-specification.md)
- [Architecture](docs/architecture.md)
- [Project structure](docs/project-structure.md)
- [Implementation plan](docs/implementation-plan.md)
- [CLI contract](docs/cli-reference.md)
- [Model format](docs/model-format.md)
- [Template authoring](docs/template-authoring-guide.md)
- [Development guide](docs/development-guide.md)
- [Agent integration](docs/agent-integration.md)
- [Codex and cloud execution](docs/codex-cloud.md)
- [Branching and releases](docs/branching-and-release.md)
- [Architecture decisions](docs/adr/README.md)

## License

Akode.DocxGen is licensed under the [MIT License](LICENSE). Dependencies must
use only MIT, BSD-2-Clause, BSD-3-Clause, or Apache-2.0 licenses.
