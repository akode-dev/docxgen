# Akode.DocxGen

`Akode.DocxGen` is a cross-platform .NET 10 CLI that turns governed JSON and
Markdown content into a polished Word document and can extract a semantic
Markdown draft from an existing DOCX:

```text
DOCX template + model.json + Markdown/assets -> validated DOCX
DOCX -> Markdown + embedded image assets
```

Phase 1 is released. The tool supports template inspection, model
scaffolding and validation, Markdown rendering, template-less conversion,
Open XML validation, images, tables, collections, automatic TOC refresh, and
optional document-version suffixes. The next minor release adds semantic
DOCX-to-Markdown extraction.

The core design rule is simple: content lives in Markdown/JSON; branding and
page layout live in the Word template.

## Inspect versus render

| Command | Purpose | Writes a DOCX |
|---|---|---:|
| `inspect` | Reads the template contract: placeholder paths, kinds, loops, required styles, template ID/version/hash | No |
| `render` | Validates inputs, binds JSON/Markdown, expands loops and Markdown, preserves template pages, post-processes fields/properties, and optionally validates OOXML | Yes |

An AI agent should call `inspect` before authoring a model. It should never
guess placeholder names.

## Document topology

A normal governed template contains:

1. a cover page with fixed artwork/logo and scalar placeholders;
2. an optional Document Control page and revision-history loop;
3. a real Word table of contents;
4. one or more Markdown body slots;
5. a preserved closing page.

The number of chapters and paragraphs is not encoded in JSON. Long-form
content is ordinary Markdown; headings naturally create as many chapters and
subchapters as needed. The body expands across pages while the template keeps
its cover, section geometry, headers, footers, TOC, and final page.

The included `templates/proposal.docx` is a synthetic, non-confidential
reference template. An official corporate template can replace it after the
same inspect/schema/Word acceptance gate.

## Quick start

Prerequisites:

- .NET SDK selected by `global.json`;
- Git;
- Microsoft Word only for the final human field refresh/acceptance step.

Verify the repository:

```powershell
./eng/verify.ps1
```

Inspect the reference template:

```powershell
dotnet run --project src/Akode.DocxGen.Cli -- inspect `
  --template templates/proposal.docx `
  --json
```

Generate an editable model:

```powershell
dotnet run --project src/Akode.DocxGen.Cli -- scaffold-model `
  --template templates/proposal.docx `
  --out artifacts/model.json `
  --with-markdown-stubs
```

Validate before rendering:

```powershell
dotnet run --project src/Akode.DocxGen.Cli -- validate-model `
  --template templates/proposal.docx `
  --model samples/model.json `
  --assets-dir samples `
  --json
```

Render and validate:

```powershell
dotnet run --project src/Akode.DocxGen.Cli -- render `
  --template templates/proposal.docx `
  --model samples/model.json `
  --assets-dir samples `
  --out artifacts/Proposal.docx `
  --append-document-version `
  --validate `
  --overwrite `
  --json
```

For `data.ds.Document.Version = "1.0"`, the last command writes
`artifacts/Proposal-v1.0.docx`. The tool never increments the business version
and never overwrites an existing file unless `--overwrite` is explicit.

Extract an existing document for agent-friendly editing:

```powershell
dotnet run --project src/Akode.DocxGen.Cli -- extract `
  --file artifacts/Proposal-v1.0.docx `
  --out artifacts/Proposal-v1.0.md `
  --json
```

Embedded images are written to `artifacts/Proposal-v1.0.assets/` by default.
Extraction preserves document meaning rather than Word layout: headings,
paragraphs, inline emphasis/code, links, lists, quotes, code blocks, tables,
and embedded images are represented in portable Markdown. Headers, footers,
cover positioning, floating layout, and generated fields such as a TOC are
not round-tripped.

## Agent-friendly JSON

The base schema is
`docs/schemas/docxgen-model-1.0.schema.json`. Each governed template adds an
adjacent schema, for example:

```text
templates/proposal.docx
templates/proposal.schema.json
```

The adjacent schema owns the exact required fields, collection shapes,
template identity, and template hash. This makes a validation hook deterministic
and gives the agent exact JSON Pointer paths and actionable hints.

A shortened model looks like this:

```json
{
  "$schema": "../docs/schemas/docxgen-model-1.0.schema.json",
  "modelVersion": "1.0",
  "template": {
    "id": "akode-proposal-reference",
    "version": "1.0.0"
  },
  "options": {
    "culture": "en-US",
    "strict": true,
    "headingOffset": 0,
    "allowRawHtml": false,
    "allowRemoteImages": false,
    "updateFieldsOnOpen": true
  },
  "data": {
    "ds": {
      "Document": {
        "Title": "Customer Platform Proposal",
        "Description": "Technical and commercial proposal",
        "Project": "Customer Platform Modernization",
        "Client": "Example Corporation",
        "Version": "1.0",
        "Status": "Draft",
        "Date": "2026-07-24",
        "Classification": "INTERNAL",
        "Author": {
          "FirstName": "Sample",
          "LastName": "Author",
          "Role": "Solution Architect",
          "Email": "sample.author@example.test"
        }
      },
      "Revisions": [
        {
          "Version": "1.0",
          "Date": "2026-07-24",
          "Author": "Sample Author",
          "Description": "Initial version"
        }
      ],
      "Body": {
        "$mdFile": "proposal.md"
      }
    }
  }
}
```

Supported value directives:

- `{ "$md": "# Inline Markdown" }`;
- `{ "$mdFile": "sections/approach.md" }`;
- `{ "$file": "assets/client-logo.png" }`;
- `{ "$text": "*literal, not Markdown*" }`.

Model options act as defaults. An explicitly supplied CLI option wins. Content
source precedence is:

```text
--set override > model.json > section-anchored Markdown
```

## Markdown and images

The Phase 1 renderer supports:

- Heading 1 through Heading 6 with an optional offset;
- paragraphs, bold, italic, strikethrough, inline code, and links;
- ordered, unordered, and nested lists using native Word numbering;
- fenced code blocks and block quotes;
- GFM pipe tables with explicit Word table geometry;
- horizontal rules;
- local PNG, JPEG, GIF, BMP, and SVG images with alt text and aspect-ratio
  preservation.

Local images resolve below `--assets-dir` and cannot escape it:

```markdown
## Architecture

![Document generation pipeline](architecture.svg "System flow")
```

Rendering is offline by default. `--allow-remote-images` is an explicit
opt-in: downloads have a 30-second timeout, size/media-type limits, no
redirects, and reject hosts resolving to private, loopback, link-local, or
multicast addresses. Local assets remain preferable for reproducible builds.

Raw HTML is removed by default. `--allow-raw-html` preserves it as reviewed
text; it is not interpreted as arbitrary OOXML.

## Section-anchored Markdown

An agent may maintain one large Markdown file and map sections to template
paths:

```markdown
<!-- docxgen:section ExecutiveSummary -->

# Executive Summary

Content...

<!-- docxgen:section ds.Approach -->

# Approach

More content...
```

Unqualified names resolve below `ds`. The parser ignores marker-shaped text
inside code, rejects duplicate/overlapping paths, and supports a
`format=table columns=...` anchor for model collections.

## Template placeholders

Examples:

```text
{{ds.Document.Title}}
{{ds.Body}:MD}
{{ds.ClientLogo}:IMG(alt=Client logo)}
{{#ds.Revisions}} ... {{/ds.Revisions}}
```

A block Markdown placeholder must occupy an otherwise empty paragraph. Fixed
logos and designed artwork normally stay in the template. See
[template authoring](docs/template-authoring-guide.md).

## Commands

| Command | Result |
|---|---|
| `inspect` | Template contract, diagnostics, identity, hash, and styles |
| `scaffold-model` | `model.json` plus optional Markdown stubs |
| `validate-model` | Hook-friendly model/Markdown/assets preflight |
| `render` | Final template-based DOCX |
| `convert` | Standalone Markdown-to-DOCX draft, optionally using reference styles and TOC |
| `extract` | Semantic DOCX-to-Markdown conversion with embedded image export |
| `validate` | Open XML SDK validation of an existing DOCX |

Every command is non-interactive. `--json` writes a versioned report to
stdout; human output uses stderr. Exit codes and diagnostic codes are stable.
See the [CLI reference](docs/cli-reference.md) and
[report contract](docs/report-format.md).

## Install and release artifacts

Create and install the local dotnet tool:

```powershell
dotnet pack src/Akode.DocxGen.Cli `
  --configuration Release `
  --output artifacts/packages

dotnet tool install Akode.DocxGen.Cli `
  --tool-path artifacts/tools `
  --add-source artifacts/packages `
  --version 1.0.0

artifacts/tools/docxgen --help
```

`.github/workflows/release.yml` creates the dotnet tool and self-contained
single-file executables for `win-x64`, `linux-x64`, and `osx-x64`. It runs only
for an explicit workflow dispatch or a `v*` tag. See
[operations and upgrades](docs/operations-and-upgrade.md).

## Development and agents

- Codex reads [`AGENTS.md`](AGENTS.md).
- Claude Code reads [`CLAUDE.md`](CLAUDE.md), which imports the shared rules.
- Hooks live under `eng/hooks` and call `validate-model`; they do not duplicate
  validation logic.
- `main` is stable; `develop` is integration; short-lived work branches are
  created from `develop`.

Useful documents:

- [Technical specification](docs/technical-specification.md)
- [Architecture](docs/architecture.md)
- [Model format](docs/model-format.md)
- [Template authoring](docs/template-authoring-guide.md)
- [Agent guide](docs/agent-guide.md)
- [Project structure](docs/project-structure.md)
- [Branching and release](docs/branching-and-release.md)
- [Implementation record](docs/implementation-plan.md)

## License

Akode.DocxGen is MIT licensed. NuGet dependencies are locked and checked
against the reviewed permissive-license inventory in
`eng/package-license-allowlist.json` and
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
