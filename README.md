# DocxGen

[![CI](https://github.com/akode-dev/docxgen/actions/workflows/ci.yml/badge.svg)](https://github.com/akode-dev/docxgen/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/akode-dev/docxgen)](https://github.com/akode-dev/docxgen/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

DocxGen turns Markdown and structured JSON into polished Word documents using
your own `.docx` template. It can inspect placeholders, generate a JSON Schema
for an AI agent, validate inputs, render the final document, and extract
semantic Markdown from an existing DOCX.

The design rule is intentionally simple:

```text
content in Markdown/JSON + design in DOCX = repeatable Word publishing
```

DocxGen is a cross-platform .NET 10 project. Use it as:

- the `docxgen` CLI or a standalone executable;
- an allowed `DocxGen` tool in an AI-agent workflow;
- the `Akode.DocxGen` NuGet library inside a .NET application.

It runs locally, requires no LLM, and keeps remote images and raw HTML disabled
unless the caller explicitly enables them.

## What it can build

A template may be as small as one placeholder or as elaborate as a corporate
report with a cover, logos, Document Control, revision tables, headers,
footers, a Word table of contents, multiple Markdown sections, images, and a
closing page.

The number of chapters is not fixed in JSON. Headings, paragraphs, lists,
tables, code blocks, and images live in Markdown and flow naturally across as
many pages as needed. Template placeholders provide the structured values:

```text
{{ds.Document.Title}}
{{ds.Body}:MD}
{{ds.ClientLogo}:IMG(alt=Client logo)}
{{#ds.Revisions}} ... {{/ds.Revisions}}
```

The bundled template and sample are synthetic and safe to reuse:

```text
templates/proposal.docx + samples/model.json + samples/proposal.md
```

## Install

Install the .NET tool:

```shell
dotnet tool install --global Akode.DocxGen.Tool
docxgen --help
```

Or download a self-contained archive from
[GitHub Releases](https://github.com/akode-dev/docxgen/releases). Release
artifacts are built for:

| Operating system | Architectures | Executable |
|---|---|---|
| Windows | x64, ARM64 | `docxgen.exe` |
| Linux | x64, ARM64 | `docxgen` |
| macOS | Intel x64, Apple Silicon ARM64 | `docxgen` |

To embed DocxGen in a .NET solution:

```shell
dotnet add package Akode.DocxGen
```

The package exposes `Akode.DocxGen.DocxGenPipelineFactory` and the public
contracts from `Akode.DocxGen.Core`. See the
[embedding guide](docs/embedding.md).

## Quick start

First inspect the template instead of guessing its placeholders:

```shell
docxgen inspect --template templates/proposal.docx --json
```

Generate a Draft 2020-12 JSON Schema that an agent or form can follow:

```shell
docxgen generate-schema \
  --template templates/proposal.docx \
  --out artifacts/proposal.schema.json \
  --template-id proposal \
  --template-version 1.0.0 \
  --overwrite \
  --json
```

Create an editable model with Markdown stubs:

```shell
docxgen scaffold-model \
  --template templates/proposal.docx \
  --out artifacts/model.json \
  --with-markdown-stubs
```

Validate the completed model and its local assets:

```shell
docxgen validate-model \
  --template templates/proposal.docx \
  --model samples/model.json \
  --assets-dir samples \
  --json
```

Render and validate the final document:

```shell
docxgen render \
  --template templates/proposal.docx \
  --model samples/model.json \
  --assets-dir samples \
  --out artifacts/Proposal.docx \
  --append-document-version \
  --validate \
  --overwrite \
  --json
```

With document version `1.0`, this writes `Proposal-v1.0.docx`. DocxGen never
increments a business version and does not overwrite an existing file unless
`--overwrite` is present.

Convert or extract without a governed template:

```shell
docxgen convert \
  --markdown samples/proposal.md \
  --out artifacts/Draft.docx \
  --overwrite

docxgen extract \
  --file artifacts/Draft.docx \
  --out artifacts/Draft.md \
  --overwrite \
  --json
```

Embedded images are exported beside extracted Markdown. Extraction preserves
document meaning—headings, text formatting, links, lists, tables, code, and
images—not Word's page geometry.

## Commands

| Command | Purpose |
|---|---|
| `inspect` | Report placeholders, loops, styles, diagnostics, identity, and hash |
| `generate-schema` | Derive a deterministic JSON Schema from template markers |
| `scaffold-model` | Create `model.json` and optional Markdown section stubs |
| `validate-model` | Preflight JSON, Markdown, assets, and template bindings |
| `render` | Generate a template-based DOCX |
| `convert` | Convert standalone Markdown to a DOCX draft |
| `extract` | Convert DOCX body content to Markdown and embedded image files |
| `validate` | Validate an existing DOCX with Open XML SDK |

Every command is non-interactive. With `--json`, stdout contains a stable,
versioned report suitable for tools and stderr remains human-readable. See the
[CLI reference](docs/cli-reference.md), [diagnostics](docs/diagnostics.md), and
[report contract](docs/report-format.md).

## Agent workflow

Register the executable under the friendly tool name `DocxGen`, but invoke the
actual binary as `docxgen`. Give the agent access only to the commands required
by its task. A reliable document-generation sequence is:

```text
inspect -> generate-schema/check -> scaffold-model -> validate-model -> render
```

The schema removes placeholder guesswork, while `validate-model --json`
provides exact JSON Pointer paths and remediation hints. Hooks under
`eng/hooks/` expose the same validation contract to local and hosted agents.
See [agent integration](docs/agent-integration.md).

## JSON and Markdown

Each governed template can have an adjacent schema:

```text
templates/report.docx
templates/report.schema.json
```

The schema owns the exact required object shape, template ID/version, and DOCX
hash. A model can reference Markdown and assets without placing large content
inside JSON:

```json
{
  "modelVersion": "1.0",
  "template": {
    "id": "proposal",
    "version": "1.0.0"
  },
  "data": {
    "ds": {
      "Document": {
        "Title": "Platform modernization",
        "Version": "1.0"
      },
      "Body": {
        "$mdFile": "proposal.md"
      }
    }
  }
}
```

Supported directives include:

- `{ "$md": "# Inline Markdown" }`;
- `{ "$mdFile": "sections/approach.md" }`;
- `{ "$file": "assets/client-logo.png" }`;
- `{ "$text": "*literal, not Markdown*" }`.

Markdown supports headings, paragraphs, emphasis, links, nested lists, fenced
code, block quotes, GFM tables, horizontal rules, and local PNG, JPEG, GIF,
BMP, or SVG images. Local paths are contained below `--assets-dir`.

## Project layout

```text
src/         production libraries and CLI
tests/       automated tests and architecture/license gates
templates/   reusable governed DOCX templates and schemas
samples/     runnable Markdown, JSON, and image inputs
docs/        public guides, contracts, architecture, and ADRs
eng/         verification scripts and agent hooks
research/    isolated experiments excluded from the production solution
```

The production packages are:

| Package | Use |
|---|---|
| `Akode.DocxGen` | Ready-to-use DOCX pipeline for .NET applications |
| `Akode.DocxGen.Core` | Technology-neutral contracts and orchestration |
| `Akode.DocxGen.Tool` | The `docxgen` command-line tool |

Architecture boundaries and the full directory map are documented in
[architecture](docs/architecture.md) and
[project structure](docs/project-structure.md).

## Contributing

The project welcomes bug reports, template scenarios, documentation
improvements, tests, and focused pull requests. Start with
[CONTRIBUTING.md](CONTRIBUTING.md) and the
[development guide](docs/development-guide.md).

```shell
dotnet restore DocxGen.sln
dotnet build DocxGen.sln --configuration Release --no-restore
dotnet test DocxGen.sln --configuration Release --no-build
```

Please report vulnerabilities privately according to
[SECURITY.md](SECURITY.md). Community participation is governed by the
[Code of Conduct](CODE_OF_CONDUCT.md).

## License

DocxGen is available under the [MIT License](LICENSE). Runtime dependencies are
locked, license-reviewed, and listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
