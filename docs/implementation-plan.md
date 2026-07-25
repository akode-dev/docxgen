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
- self-contained single-file release workflow for Windows, Linux, and macOS on
  x64 and ARM64;
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

Implemented after the 1.0 release:

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

### P7 — deterministic template schema generation

Selected on 2026-07-25. The complete acceptance slice is:

#### P7.1 — Contract and architecture

- add `generate-schema` as a public non-interactive command;
- keep `inspect --schema-out` as the existing inspection DTO contract;
- generate a self-contained Draft 2020-12 schema from one DOCX template;
- record the decision and inference boundaries in ADR-0008.

#### P7.2 — Hierarchical template discovery

- preserve existing flat placeholder diagnostics and locations;
- add a technology-neutral object/collection/value shape to `TemplateSchema`;
- use DocxTemplater's static schema analysis for nested objects, collections,
  conditions, switches, and expressions;
- overlay `:MD` and `:IMG` semantic kinds discovered from template markers;
- retain deterministic fallback construction from flat placeholders.

#### P7.3 — Schema and model generation

- infer text, Markdown, binary, object, and array schemas;
- make every statically reachable binding required, matching strict rendering;
- constrain template ID/version and embed the template hash;
- scaffold nested objects and nested collection items from the same shape;
- emit no business formats, enums, ranges, or defaults that are not encoded in
  the template.

#### P7.4 — CLI and automation

- support `--template-id`, `--template-version`, `--overwrite`, and `--json`;
- support `--check` for non-mutating CI drift detection;
- add typed report data, stable `SCH` diagnostics, and report-schema coverage;
- preserve atomic output and stable exit-code behavior.

#### P7.5 — Arbitrary-topology acceptance

- test a one-scalar template and a one-`:MD` template;
- test nested objects, nested collections, images, headers/footers, and
  conditional/expression references;
- prove generated-schema validation, scaffold generation, and rendering;
- reclassify cover, Document Control, TOC, and closing page as proposal
  recommendations rather than universal template requirements;
- pass restore, Release build, all tests, and CLI generation/check smoke tests.

P7 does not infer dates, email formats, enums, descriptions, optional business
fields, or page layout from labels or visual appearance. Those constraints
require an explicit future annotation contract.

P7 implementation is complete on 2026-07-25. Release restore/build/test passes
with 100 tests, and CLI smoke coverage proves generation, successful
non-mutating `--check`, `E-SCH-002` drift failure, and validation of the
reference model against a generated adjacent schema.

### Remaining backlog

- MCP stdio adapter over the same Core;
- RTL/Arabic rendering and a dedicated template;
- governed PDF export strategy;
- DOCX semantic diff;
- advanced captions and cross-references.

### P8 — Open-source productization and public packaging

Selected on 2026-07-25. The complete acceptance slice is:

- publish the product as **DocxGen**, with `docxgen`/`docxgen.exe` executable
  names and `Akode.DocxGen.*` .NET namespaces;
- move the renderer experiment under `research/` and keep it outside the
  production solution;
- provide the `Akode.DocxGen` runtime package, `Akode.DocxGen.Core` contracts
  package, and `Akode.DocxGen.Tool` CLI package;
- expose a ready-to-use public pipeline factory for application embedding;
- produce self-contained x64 and ARM64 archives for Windows, Linux, and macOS;
- rewrite the root README around CLI, AI-agent, and library use cases;
- add contribution, conduct, security, support, issue, and pull-request
  guidance;
- update the reference template, schemas, agent permissions, package metadata,
  release workflow, and every public document to the shared naming contract;
- prove naming consistency with a repository scan, package/tool smoke tests,
  complete solution verification, structural DOCX checks, and page-by-page
  visual review.

Because namespace, package, executable, schema-extension, environment-variable,
and Markdown-anchor names are public API, P8 is a major product release.

P8 implementation is complete on 2026-07-25. Verification covers:

- clean Release restore/build and 102 automated tests;
- `dotnet format --verify-no-changes`;
- all three 2.0.0 NuGet packages and an external package-consumer smoke test;
- local installation and execution of `Akode.DocxGen.Tool`, including
  `docxgen --version`, template inspection, and model validation;
- self-contained single-file publication for all six approved x64/ARM64 RIDs;
- naming-consistency scans across source, DOCX XML, package contents, and
  executables;
- structural template integration plus visual inspection of all five raw
  template pages.
