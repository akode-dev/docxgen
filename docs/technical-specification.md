# Akode.DocxGen technical specification

## 1. Document status

| Field | Value |
|---|---|
| Product | Akode.DocxGen |
| Target | .NET 10 / C# 14 |
| Status | Phase 1 released; extraction and template-schema generation implemented on `develop` |
| Primary users | Bid teams, developers, CI, coding agents |
| Runtime model | Offline deterministic CLI |
| License policy | MIT/BSD/Apache-2.0 only |

This document is the normative product specification. ADRs may refine an
implementation decision, but a change to a public CLI contract, exit code,
model schema, or security default requires an explicit specification update.

## 2. Problem

Proposal content is assembled from customer documents, internal material, and
subject-matter input. AI agents are effective at drafting and revising that
content in text files, but producing a polished corporate DOCX repeatedly
causes agents to create one-off scripts, install dependencies, and manipulate
OOXML inconsistently.

Akode.DocxGen provides deterministic forward generation and semantic reverse
extraction:

```text
(DOCX template, validated model, Markdown, local assets) -> DOCX
placeholder-bearing DOCX template -> Draft 2020-12 model schema
DOCX -> Markdown + embedded image assets
```

The tool does not generate business content and does not call an LLM.

## 3. Product principles

1. Content lives in Markdown and JSON.
2. Branding and page layout live in the DOCX template.
3. The template/model contract is inspectable before rendering.
4. Every failure is machine-readable and actionable by an agent.
5. Strict, offline, path-contained behavior is the default.
6. The same Core powers CLI, tests, and the future MCP adapter.
7. The output is deterministic after normalization.
8. A valid OOXML package is necessary but not sufficient; representative
   documents require rendered visual review.

## 4. Recommended proposal shape

The reference proposal template contains:

1. cover section with fixed branding and scalar placeholders;
2. optional Document Control page;
3. a real Word TOC field;
4. a standalone `{{ds.Body}:MD}` paragraph;
5. a next-page section break;
6. final branded page.

The fixed Akode logo is embedded in the template. Variable images use model
or Markdown assets.

`ds.Body` supports an arbitrary number of chapters and paragraphs. For this
single-body mode, Markdown H1 maps to Word Heading 1 and `headingOffset` is
zero. If a template contains fixed Heading 1 section titles and separate
Markdown slots underneath, the section fragment uses a positive heading
offset.

This proposal topology is not a universal template contract. The engine also
supports a one-scalar template, a template containing only
`{{ds.Body}:MD}`, multiple independent body slots, nested objects and
collections, headers/footers, and conditional branches.

## 5. Scope

### 5.1 Phase 1

- cross-platform .NET CLI;
- DOCX placeholder inspection;
- model scaffold generation;
- base and template-specific JSON Schema validation;
- hook-friendly `validate-model`;
- Markdown body and anchored-section preprocessing;
- local images, links, lists, tables, quotes, and code;
- model collections and conditional blocks;
- rendering through a proven permissive OSS engine;
- post-processing through Open XML SDK;
- OOXML validation;
- atomic file output;
- optional document version suffix;
- JSON reports and stable exit codes;
- dotnet tool and self-contained packages;
- Windows, Linux, and macOS CI;
- template-authoring and agent-integration documentation.

### 5.2 Phase 2

- semantic DOCX-to-Markdown extraction from the main document body (selected
  and implemented);
- deterministic JSON Schema generation from placeholder-bearing templates
  (selected and implemented);
- MCP server over Core;
- RTL/Arabic after a dedicated spike;
- PDF delivery after implementation and license review;
- textual DOCX comparison;
- richer semantic figure hints and cross-references.

### 5.3 Non-goals

- LLM content generation;
- Markdown editor or web UI;
- pixel-perfect or layout-preserving DOCX-to-Markdown synchronization;
- tracked-change round-trip to Markdown;
- Office Interop, COM automation, or headless Word;
- arbitrary floating-object layout authored in Markdown;
- network access during a default render;
- automatic document-version increments.

## 6. Public CLI

### 6.1 Common behavior

- No interactive prompts.
- stdout contains JSON with `--json`; otherwise only the output path or compact
  result.
- stderr contains logs.
- JSON errors are emitted even when the process exits non-zero.
- All commands accept absolute and relative paths.
- Output directories are created when safe.
- Existing outputs require `--overwrite`.

### 6.2 `inspect`

```text
docxgen inspect
  --template <template.docx>
  [--schema-out <inspection.json>]
  [--include-text-probe]
  [--json]
```

Responsibilities:

- validate the ZIP/OOXML package type;
- reject macro-enabled input;
- calculate template hash;
- enumerate placeholders in body, headers, footers, and text boxes;
- infer scalar, Markdown, image, object, and collection shapes;
- report formatter names and arguments;
- detect suspicious split/unclosed placeholders;
- check the required style contract;
- reconcile discovered placeholders with an adjacent template schema;
- optionally emit the inspection DTO for tooling.

`inspect` never renders or modifies the template.

### 6.3 `generate-schema`

```text
docxgen generate-schema
  --template <template.docx>
  --out <template.schema.json>
  [--template-id <id>]
  [--template-version <version>]
  [--check]
  [--overwrite]
  [--json]
```

The command statically derives a self-contained Draft 2020-12 model contract
from scalar, object, collection, conditional, switch, expression, Markdown,
and image bindings in all inspected document parts. Every reachable binding
is required, matching strict-mode preflight and union-of-branches analysis.
The schema contains exact template identity, version, and hash metadata.

Generation is deterministic. `--check` performs a non-mutating normalized-text
comparison suitable for CI. The command never infers business formats, enums,
ranges, defaults, descriptions, or semantic optionality from Word labels or
visual layout.

### 6.4 `scaffold-model`

```text
docxgen scaffold-model
  --template <template.docx>
  --out <model.json>
  [--with-markdown-stubs]
  [--force]
```

The generated model contains `$schema`, model contract version, template
identity/version, empty required values, and `$comment` guidance.

### 6.5 `validate-model`

```text
docxgen validate-model
  --template <template.docx>
  --model <model.json>
  [--markdown <proposal.md>]
  [--assets-dir <directory>]
  [--strict | --lenient]
  [--json]
```

This is the preferred lifecycle-hook command. It performs all checks that do
not require document generation:

- JSON syntax;
- base DocxGen schema;
- template-specific schema;
- template identity/hash compatibility;
- required non-empty values;
- scalar/collection/object kind matching;
- unknown directives;
- anchored-section uniqueness;
- bound, unbound, and extra paths;
- local asset existence, media type, size, and path containment;
- remote image and raw HTML policy;
- Markdown preprocessing diagnostics.

### 6.6 `render`

```text
docxgen render
  --template <template.docx>
  [--model <model.json>]
  [--markdown <proposal.md>]
  --out <output.docx>
  [--assets-dir <directory>]
  [--strict | --lenient]
  [--culture <ietf>]
  [--heading-offset <integer>]
  [--allow-raw-html]
  [--allow-remote-images]
  [--update-fields-on-open | --no-update-fields-on-open]
  [--set <path=value>]...
  [--doc-property <name=value>]...
  [--append-document-version]
  [--validate]
  [--dry-run]
  [--overwrite]
  [--json]
```

At least one of `--model` and `--markdown` is required. They may be supplied
together. Precedence is:

```text
--set > model.json > anchored Markdown
```

`--dry-run` executes the complete pipeline through reconciliation and renderer
preflight but does not write the final path.

`--append-document-version` reads `data.ds.Document.Version`. Given output
`Proposal.docx` and version `3.0`, it resolves `Proposal-v3.0.docx`.

### 6.7 `convert`

Creates an unbranded draft from Markdown and an optional style reference.
Production proposals should use `render`.

### 6.8 `extract`

Extracts the main DOCX body into portable Markdown and exports embedded images:

```text
docxgen extract
  --file <input.docx>
  --out <output.md>
  [--assets-dir <directory>]
  [--overwrite]
  [--json]
```

The default assets directory is `<output-name>.assets` beside the Markdown
file. Paths embedded in Markdown are relative to the Markdown output.
Extraction preserves supported document semantics, not Word layout. It
includes paragraphs, Heading 1–6, inline formatting/code, links, hard line
breaks, ordered/unordered nested lists, quote/code/caption styles, GFM tables,
horizontal rules, and embedded images. Generated fields and unsupported Word
constructs are omitted or downgraded with stable diagnostics. Headers,
footers, comments, footnotes, and tracked deletions are outside the initial
contract.

### 6.9 `validate`

Validates an existing DOCX using Open XML SDK and product-specific checks for
leftover placeholders, broken relationships, and required package parts.

## 7. Exit codes

| Code | Meaning |
|---:|---|
| 0 | Success |
| 1 | Unexpected product error |
| 2 | Invalid usage or input/output path |
| 3 | Template error |
| 4 | Model error |
| 5 | Rendering error |
| 6 | Output validation error |
| 7 | I/O error |

Diagnostic string codes remain additive and stable within a major version.
Every error has `message`, `hint`, and, when applicable, JSON `path`.

## 8. JSON contracts

### 8.1 Version separation

- tool version: binary/package release;
- model schema version: structure of `model.json`;
- template version: design and placeholder contract;
- document version: business document revision.

These values must never be inferred from each other.

### 8.2 Model directives

| Directive | Meaning |
|---|---|
| `$md` | Inline Markdown |
| `$mdFile` | Markdown loaded under `assets-dir` |
| `$file` | Local binary/image asset |
| `$text` | Explicit plain text |

Unknown `$` keys are errors.

### 8.3 Template-specific schema

A production template is shipped as:

```text
proposal.docx
proposal.schema.json
```

`generate-schema` derives required values, structural types, nested collection
item shapes, template identity/version, and the `x-docxgen-templateHash`
extension directly from placeholder-bearing DOCX content. `inspect` detects
stale adjacent contracts.

All statically reachable bindings are required because static template
analysis cannot prove runtime branch reachability. Business formats, enums,
ranges, descriptions, defaults, and optional business semantics cannot be
inferred reliably from placeholder text; they require a future explicit
annotation contract or deliberate schema review.

### 8.4 Machine-readable command reports

Every `--json` response uses report contract `1.0` and the same top-level
envelope:

```json
{
  "reportVersion": "1.0",
  "command": "validate-model",
  "ok": false,
  "exitCode": 4,
  "errorCode": "E-MDL-002",
  "message": "Model validation failed.",
  "hint": "Update the reported value to satisfy the adjacent template schema.",
  "diagnostics": [
    {
      "code": "E-MDL-002",
      "severity": "error",
      "message": "The model violates the schema.",
      "hint": "Update the reported value to satisfy the adjacent template schema.",
      "path": "/data/ds/Document/Title"
    }
  ]
}
```

`exitCode` is numeric. Diagnostic severities and model value kinds are
lower-camel-case strings. Successful reports have exit code `0`, omit
`errorCode` and `hint`, contain no error diagnostics, and include typed
command-specific `data`. Failed reports have a non-zero exit code, promote the
first error diagnostic to `errorCode` and `hint`, and omit `data`.

The normative schema is
`docs/schemas/docxgen-report-1.0.schema.json`. Breaking field changes require a
new report major version; additive optional data may remain within `1.x`.

## 9. Markdown contract

Phase 1 supports:

- headings H1-H6 with configurable offset;
- paragraphs;
- bold, italic, strikeout, and inline code;
- nested ordered and unordered lists;
- GFM pipe tables;
- block quotes;
- fenced code;
- links;
- local images;
- horizontal rules or explicit page-break semantics;
- section anchors in HTML comments.

Not supported in Phase 1:

- math;
- Mermaid rendering;
- true Word footnotes;
- arbitrary inline HTML by default;
- arbitrary CSS or floating layout;
- remote images by default.

All transformations operate on a Markdig AST, not whole-document regexes.

### 9.1 Section-anchored Markdown

One Markdown file may populate several template paths:

```markdown
<!-- docxgen:section ExecutiveSummary -->

Akode proposes a **digital platform**.

<!-- docxgen:section ds.Approach -->

## Delivery approach

1. Discovery
2. Foundation

<!-- docxgen:section Team format=table columns=Name,Role -->

| Name | Role |
|---|---|
| Alexei | Solution Architect |

<!-- docxgen:end -->
```

The normative comment marker is:

```text
<!-- docxgen:section <Name> [key=value ...] -->
```

- `docxgen:section` and `docxgen:end` are case-insensitive; `Name` and resolved
  model paths are case-sensitive;
- names match `[A-Za-z_][A-Za-z0-9_.]{0,127}`;
- an unqualified name resolves under `ds`; a dotted name is already absolute;
- a section ends at the next section marker, `docxgen:end`, or EOF;
- content before the first marker is ignored with `W-MD-005`;
- duplicate or structurally overlapping paths fail with `E-MDL-005`;
- markers inside fenced code blocks or inline code are content, not anchors;
- UTF-8 BOM and CRLF are accepted, output blocks use LF, and only blank edge
  lines are trimmed.

`format=table` requires `columns=Name,Role,...` and exactly one GFM pipe table.
Each data row becomes an object in a collection, with cells mapped by position
to the case-sensitive column names. Invalid table contracts fail with
`E-MD-006`.

Core merges sources recursively with this precedence:

```text
--set > model.json > anchored Markdown
```

An explicit model leaf that replaces anchored content emits `W-MRG-001`.
`--set` infers JSON numbers, booleans, and null; other values are strings.
Prefix the value with `@` to force a string, for example
`--set ds.Code=@0042`.

## 10. Images and figures

- Corporate branding images remain in the template.
- Markdown image paths resolve relative to the source file or explicit assets
  root and must remain inside that root.
- Initial formats are PNG and JPEG; other formats require tested support.
- Images are inserted inline, preserve aspect ratio, and are clamped to the
  available content width.
- Alt text comes from Markdown.
- Caption support uses the template's `Caption` style.
- Renderer behavior at page boundaries must be covered by visual fixtures.
- Remote images are rejected unless explicitly enabled and must still obey
  size, media type, and timeout limits.

Semantic size presets such as `small`, `medium`, and `full` are preferred over
arbitrary width coordinates in Markdown.

## 11. Template contract

Templates must define:

- `Normal`;
- `Heading1` through at least `Heading4`, preferably `Heading6`;
- `ListParagraph`;
- `TableGrid` or `AkodeTable`;
- `Quote`;
- paragraph `Code`;
- character `CodeInline`;
- character `Hyperlink`;
- `Caption`.

Templates also require bullet and decimal multi-level numbering definitions.
The TOC is a real field and must be protected from placeholder parsing.

Block placeholders such as `{{ds.Body}:MD}` occupy an otherwise empty
paragraph. Authors type placeholders in one operation and run `inspect
--include-text-probe` after changes.

Optional full-page sections should initially use separate template variants.
Conditionally deleting Word section boundaries is deferred until a test proves
that no blank-page or header/footer defects remain.

## 12. Render pipeline

1. Parse CLI input.
2. Load, identify, hash, and inspect the template.
3. Load model JSON.
4. Validate base and template schemas.
5. Parse anchored Markdown when present.
6. Merge Markdown, model, and CLI overrides.
7. Resolve local assets through `PathGuard`.
8. Preprocess Markdown AST.
9. Reconcile values with the template schema.
10. Render through `IDocumentRenderer`.
11. Run ordered post-processors.
12. Optionally validate OOXML.
13. Resolve versioned output name.
14. Write a same-directory temporary file and atomically move it.
15. Emit a JSON or human report.

The renderer does not choose an output path and never writes directly to the
user's final file.

## 13. Security

- no network by default;
- macro-enabled templates rejected;
- paths canonicalized before access;
- assets restricted to one approved root;
- symlink/reparse-point behavior tested on each OS;
- configurable per-file and total input limits;
- decompression size and ZIP-entry limits;
- raw HTML stripped by default;
- no template expressions beyond allow-listed formatters;
- no secrets or customer proposals in logs, fixtures, or CI artifacts;
- atomic writes;
- package-license allow-list and vulnerability scan in CI.

## 14. Non-functional requirements

| Area | Requirement |
|---|---|
| Determinism | Equal normalized inputs produce equal normalized OOXML |
| Performance | Target: 60-page proposal in at most 3 seconds and 300 MB RSS |
| Portability | Windows, Linux, and macOS without Office installation |
| Concurrency | One render per renderer instance/process until proven safe |
| Reliability | No partial output; no silent placeholder removal in strict mode |
| Observability | JSON duration, hashes, bound/unbound paths, Markdown stats |
| Compatibility | Word 2021/365 opens output without recovery |
| Accessibility | Heading hierarchy, image alt text, table header semantics |

## 15. Testing

- Core unit tests for JSON, anchors, Markdown AST, merge precedence, path
  security, diagnostics, and filename versioning;
- adapter integration tests against minimal and branded DOCX templates;
- normalized OOXML golden tests;
- CLI end-to-end tests for every exit code and JSON shape;
- architecture tests for project dependency rules;
- cross-platform CI;
- deterministic two-run comparisons;
- performance benchmark;
- visual render fixtures inspected at 100%;
- a manual Word acceptance pass for the reference template.

Fixtures are synthetic or anonymized. Never copy a confidential proposal into
the repository.

## 16. Acceptance

Phase 1 is accepted when:

1. `inspect` reports placeholders from body, header, footer, and text box.
2. `validate-model` reports missing required values with exact paths and hints.
3. A single-body Markdown proposal renders headings, nested lists, a table,
   links, code, and local images with template styles.
4. Collections and conditions render without leftover marker rows.
5. Word opens the output without repair.
6. TOC updates when Word opens the document.
7. Version suffix behavior is deterministic and tested.
8. Open XML validation reports no errors.
9. Two renders normalize identically.
10. Windows, Linux, and macOS CI pass.
11. The license gate passes.
12. A fresh Codex and Claude Code session can follow repository instructions,
    run the intended workflow, and avoid ad-hoc document scripts.

The selected Phase 2 extraction slice is accepted when a supported generated
DOCX can be extracted into Markdown with headings, inline formatting, links,
lists, quotes, tables, and deterministic image assets; unsupported fields
produce stable warnings; and the result can be passed back to `convert`.

## 17. Delivery and branches

Development starts on `develop`. Work is merged through reviewed feature
branches. `main` contains stable, releasable commits. No release package is
published before P0 confirms or supersedes the rendering-engine ADR.

See [branching and releases](branching-and-release.md) and
[implementation plan](implementation-plan.md).
