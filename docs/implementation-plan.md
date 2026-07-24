# Implementation record

Phase 1 was completed on 2026-07-24 as one vertically verified product slice.
This file records the result and separates shipped work from deferred Phase 2.

## Phase 1 acceptance status

### P0 — Rendering-engine decision

Complete. The spike retained DocxTemplater core for binding, rejected its
Markdown and Images extensions, and selected the bounded Markdig/Open XML
renderer in ADR-0005.

### P1 — Foundation

Complete:

- .NET 10 solution and project boundaries;
- stable diagnostics, exit codes, requests/results, and JSON reports;
- locked restore and permissive-license gate;
- Windows/Linux/macOS CI.

### P2 — Model, schema, Markdown, and security

Complete:

- Draft 2020-12 base model validation;
- adjacent template schema validation and hash/identity reconciliation;
- `$md`, `$mdFile`, `$file`, and `$text`;
- section anchors and table-to-collection anchors;
- source precedence and CLI overrides;
- model-option/explicit-CLI precedence;
- neutral Markdown AST, heading offset/clamp, raw HTML policy;
- local path containment and size/depth/count limits;
- offline-by-default remote image policy with bounded opt-in downloads;
- strict/lenient placeholder semantics.

The heading-style anchor alternative was removed from Phase 1: standalone
comment anchors are deterministic, invisible in rendered Markdown, and already
cover the agent workflow without coupling content to a visual style name.

### P3 — DOCX adapter

Complete:

- package-wide template inspection;
- DocxTemplater scalar/collection/conditional binding;
- Markdown formatter and Open XML block renderer;
- native headings, lists, tables, quotes, code, links, and images;
- field-update, custom-property, and leftover-marker post-processors;
- standalone Markdown conversion;
- Open XML validation.

### P4 — CLI

Complete:

- `inspect`, `scaffold-model`, `validate-model`, `render`, `convert`,
  `validate`;
- stdin model support, atomic output, dry-run, overwrite guards;
- stable JSON/human output and exit mapping;
- versioned output names.

### P5 — Reference template and samples

Complete for the synthetic reference:

- cover, Document Control, TOC, Markdown body slot, final page;
- adjacent schema and non-confidential sample;
- local Microsoft Word field refresh and PDF/page rendering;
- all six final pages visually inspected;
- Open XML, section, headings, images, fields, styles, and accessibility
  audits.

Acceptance of an official corporate Akode template remains a template-content
governance task. The product implementation does not depend on its branding.

### P6 — Packaging and release

Complete:

- NuGet dotnet tool package;
- self-contained single-file release workflow for `win-x64`, `linux-x64`,
  and `osx-x64`;
- checksums, license, third-party notices, changelog, and operations guide;
- release workflow runs only on explicit dispatch or a `v*` tag.

## Verification baseline

- `dotnet build ... --configuration Release`: zero warnings/errors;
- 80 automated tests;
- complete sample render and conversion;
- final six-page DOCX visually reviewed;
- generated and Word-refreshed DOCX packages validate with zero Open XML
  errors and warnings;
- local dotnet tool and Windows self-contained executable smoke-tested.

## Phase 2 backlog

### Selected slice — semantic DOCX extraction

Implemented for the next minor release:

- Core `IDocxMarkdownExtractor`, immutable request/result/assets/stats, and
  pipeline orchestration;
- `extract` CLI command with relative asset links, overwrite preflight,
  machine-readable report data, and stable diagnostics;
- Open XML main-body extraction for headings, paragraphs, inline formatting,
  links, lists, quotes/code/captions, tables, horizontal rules, and images;
- resource limits, invalid/macro-enabled package rejection, round-trip
  integration tests, specification, and ADR.

Exact Word layout, headers/footers, comments, footnotes, and tracked-change
round trips remain explicitly outside this slice.

### Remaining backlog

- MCP stdio adapter over the same Core;
- RTL/Arabic rendering and a dedicated template;
- governed PDF export strategy;
- DOCX semantic diff;
- advanced captions and cross-references.
