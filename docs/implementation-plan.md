# Implementation plan

Work in vertical acceptance slices. Every phase ends with executable evidence,
not only source files.

## P0 — Rendering-engine spike

Status: completed on 2026-07-24. ADR-0001 was rejected and superseded by
ADR-0005. Evidence is in `docs/spikes/p0-renderer-spike.md`.

Goal: prove or reject ADR-0001 before production architecture grows around it.

### Inputs

- anonymized Akode-branded template;
- cover, Document Control, TOC, one body marker, final page;
- Markdown containing H1-H4, nested lists, table, link, quote, code, and local
  images;
- placeholders in main body, header, footer, first-page header, and text box.

### Experiments

1. Render Markdown through DocxTemplater 2.8.3.
2. Record actual formatter syntax and API behavior.
3. Verify style mapping and numbering.
4. Determine whether heading offset must be applied to Markdig AST.
5. Verify image sizing and local resolution.
6. Verify TOC ignore behavior and `w:updateFields`.
7. Verify that the final page and section-specific headers remain correct.
8. Render short, 30-page, and 60-page fixtures.
9. Inspect package validity and every rendered page.
10. Run license inventory.

### Exit

- ADR-0001 confirmed or superseded;
- spike code/fixtures retained under `spike/`;
- limitations and workarounds documented;
- no production renderer work begins before this decision.

Exit result: complete. The spike retained DocxTemplater core for template
binding, rejected its Markdown and Images extensions, and selected a bounded
Markdig/Open XML body renderer. The real branded-template acceptance pass
remains a P5 gate because no approved Akode template was supplied.

## P1 — Foundation

- complete Core request/result contracts;
- base diagnostic registry and completeness tests;
- architecture tests;
- CI on Windows, Linux, and macOS;
- locked restores and package-license gate;
- JSON report schemas.

## P2 — Model and Markdown

- base model schema validation;
- template-specific schema reconciliation;
- `ModelJsonReader`;
- `$md`, `$mdFile`, `$file`, `$text`;
- `SectionAnchorParser`;
- table-to-collection anchors;
- AST heading offset;
- raw HTML and remote image policy;
- `PathGuard` and limits;
- model merge precedence;
- `validate-model` Core service.

Exit: Core test coverage at least 90% for these components.

## P3 — DOCX adapter

- renderer proven in P0;
- template inspector across package parts;
- formatter registration;
- update-fields and property post-processors;
- image/table geometry;
- leftover scanner;
- Open XML validation;
- normalization and golden fixtures.

## P4 — CLI

- composition root and logging;
- `inspect`;
- `scaffold-model`;
- `validate-model`;
- `render`;
- `convert`;
- `validate`;
- stdout/stderr contract;
- stable exit mapping;
- atomic output;
- versioned filename option;
- complete process-level tests.

## P5 — Reference template and samples

- approved template style contract;
- cover, Document Control, TOC, body marker, final page;
- adjacent template schema;
- representative non-confidential sample;
- Windows Word acceptance;
- rendered visual review.

## P6 — Packaging and release

- dotnet tool package;
- self-contained packages for approved RIDs;
- CI artifacts;
- changelog/release notes;
- generated third-party notices;
- signed/checksummed artifacts if required;
- operations and upgrade guide.

## Phase 2

- MCP adapter;
- RTL/Arabic spike and separate template;
- PDF strategy and legal review;
- DOCX diff;
- advanced captions and cross-references.

## Next agent tasks

1. Complete P1 diagnostic registry and completeness tests.
2. Add architecture tests for project dependency rules.
3. Add a resolved dependency-license gate.
4. Complete cross-platform CI and locked-restore validation.
5. Begin P2 only after the P1 acceptance slice is green.
