# Implementation status and roadmap

This document is the current product status and forward roadmap. Historical
design decisions live in `docs/adr/`; released behavior lives in
`CHANGELOG.md`.

## Current release

DocxGen 2.1 is a cross-platform .NET 10 CLI and embeddable library for governed
DOCX generation and semantic DOCX extraction.

Shipped capabilities:

- eight non-interactive commands: `inspect`, `generate-schema`,
  `scaffold-model`, `validate-model`, `render`, `convert`, `extract`, and
  `validate`;
- deterministic model and report contracts with stable diagnostics and exit
  codes;
- template inspection and Draft 2020-12 schema generation for scalar, object,
  collection, conditional, Markdown, and image placeholders;
- strict model/template reconciliation, local asset containment, and
  offline-by-default rendering;
- bounded Markdown-to-Open-XML rendering for headings, text formatting, links,
  lists, tables, quotes, code, horizontal rules, and images;
- semantic DOCX-to-Markdown extraction with deterministic embedded-image
  export;
- `Akode.DocxGen`, `Akode.DocxGen.Core`, and `Akode.DocxGen.Tool` packages;
- self-contained x64 and ARM64 executables for Windows, Linux, and macOS;
- agent-oriented JSON output, validation hooks, and repository guidance.

The bundled proposal template is a synthetic integration example, not a
restriction on supported document types. Templates may have arbitrary
placeholder topology and page structure.

## Release 2.1 acceptance slice

- remove historical renderer experiments and dedicated experiment
  documentation;
- remove rejected experimental packages from central package management and
  license inventory;
- describe the product generically rather than as a proposal-only generator;
- consolidate repository structure guidance into the architecture document;
- keep personal `.claude/` and `.codex/` configuration outside version
  control;
- update GitHub Actions to Node.js 24-compatible major versions;
- add opt-in NuGet.org trusted publishing through GitHub OIDC;
- preserve every public CLI, model, report, diagnostic, template, and security
  contract;
- pass locked restore, Release build, all tests, package inspection, workflow
  linting, and cross-platform release packaging.

## Maintenance policy

Defects and documentation corrections may be selected directly. A new public
feature requires:

1. a bounded acceptance slice;
2. an architecture review when project boundaries or dependencies change;
3. an ADR for a durable or hard-to-reverse design choice;
4. public contract and schema updates where applicable;
5. focused tests followed by full solution verification.

## Roadmap

Backlog items are intentionally unordered until selected:

- MCP stdio adapter over Core requests and results;
- explicit template annotations for business formats, enums, optionality,
  descriptions, and defaults;
- RTL/Arabic rendering and template acceptance;
- governed PDF export after implementation and license review;
- semantic DOCX comparison;
- advanced captions and cross-references.

The roadmap does not authorize implementation by itself. Work begins only
after the user or issue selects a specific item and acceptance criteria.
