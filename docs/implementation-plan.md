# Implementation plan

Work in vertical acceptance slices. Every phase ends with executable evidence,
not only source files.

## P0 — Rendering-engine spike

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

## First agent tasks

1. Create `feature/p0-renderer-spike` from `develop`.
2. Obtain or create the anonymized representative template.
3. Implement only the spike harness and fixtures.
4. Record results in ADR-0001.
5. Ask for an architecture decision if the spike fails.

Agents must not start a custom renderer as a fallback inside the same task.
