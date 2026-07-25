# ADR-0008: Generate JSON Schema from placeholder-bearing DOCX templates

- Status: Accepted
- Date: 2026-07-25

## Context

DocxGen can inspect template markers and scaffold a model, but its
`inspect --schema-out` artifact is an inspection DTO rather than a normative
Draft 2020-12 model schema. Manually maintained adjacent schemas make new
document types expensive and allow placeholder/schema drift.

A DOCX template can state structural facts through scalar markers, formatter
markers, loops, conditions, switches, expressions, and nesting. It cannot
reliably state business formats, enums, length limits, descriptions, or
defaults through visual layout alone.

## Decision

Add:

```text
docxgen generate-schema --template template.docx --out template.schema.json
```

The command statically analyzes only the supplied DOCX. It emits a
self-contained Draft 2020-12 schema for the DocxGen model envelope and the
template's reachable data shape.

Inference rules:

- scalar binding → non-empty primitive or explicit `$text`;
- `:MD` binding → `$md` or `$mdFile`;
- `:IMG` binding → `$file`;
- object access → closed object with discovered properties;
- loop binding → array with the discovered item shape;
- all statically reachable bindings are required, matching strict-mode
  preflight and DocxTemplater's union-of-branches analysis.

Template ID defaults to a safe form of the DOCX filename and version defaults
to `1.0.0`. Callers may override either explicitly. The generated schema
records both values and the SHA-256 template hash.

`--check` compares the deterministic generated form with an existing schema
without writing and fails with a stable schema-drift diagnostic.

## Consequences

- A minimal one-placeholder template is a supported product shape.
- Proposal cover, Document Control, TOC, and final page remain one recommended
  template topology, not an engine requirement.
- Nested model scaffolds and generated schemas share one hierarchical template
  shape.
- The existing inspection DTO remains compatible and gains additive shape
  information.
- No AI or visual-label heuristic participates in schema generation.
- Business-specific constraints may still be added deliberately after
  generation, but `--check` then compares against the generated structural
  contract and will report differences.
