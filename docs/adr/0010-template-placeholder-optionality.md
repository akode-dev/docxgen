# ADR-0010: Do not infer required fields from DOCX placeholders

- Status: Accepted
- Date: 2026-07-26
- Supersedes: the requiredness rule in ADR-0008

## Context

A DOCX placeholder states where a value can be rendered and provides enough
syntax to infer its structural type. It does not state whether the business
document permits that value to be empty. Treating every statically reachable
placeholder as required made ordinary templates unusable without a complete
JSON model and made conditional or decorative metadata unexpectedly strict.

Strict rendering, generated structural schemas, and deliberately governed
business contracts are separate concerns and need separate controls.

## Decision

`generate-schema` emits every discovered template binding as an optional
property. It continues to generate the closed object topology, directive
types, collection item shapes, template identity/version, and template hash.
The model envelope and directive internals retain their own structural
requirements.

A template owner may promote selected properties to JSON Schema `required`
arrays during deliberate contract review. Requiredness read from an adjacent
schema is authoritative and lenient rendering does not bypass it.

Strict rendering remains an explicit per-operation option that requires every
discovered placeholder regardless of generated-schema optionality.

## Consequences

- A DOCX with many cover or Document Control placeholders can render from only
  the values supplied by the caller.
- Missing optional placeholders are removed with `W-MDL-007` in lenient mode.
- Governed templates can still enforce non-empty fields, formats, enums, and
  complete collection items through their adjacent schema.
- `generate-schema --check` compares against the untouched structural
  baseline. A schema extended with business constraints is a reviewed governed
  artifact rather than a byte-for-byte generated baseline.
- This changes deterministic generated output and therefore requires updated
  schema fixtures, documentation, and compatibility notes.
