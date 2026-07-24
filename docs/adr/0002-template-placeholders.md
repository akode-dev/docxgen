# ADR-0002: Text placeholders in DOCX templates

- Status: Accepted
- Date: 2026-07-24

## Decision

Use DocxTemplater text placeholders such as `{{ds.Document.Title}}` and
standalone Markdown placeholders such as `{{ds.Body}:MD}`.

Do not make Word content controls the primary template contract in Phase 1.

## Rationale

Text placeholders are compatible with the selected engine, visible to template
authors, statically inspectable, and support loops and formatters. The inspector
must detect split/unclosed placeholders and the authoring guide must minimize
Word run-splitting risk.

## Consequences

Requiredness and precise data types are not reliably inferable from placeholder
text, so templates also ship a JSON Schema under ADR-0003.
