# ADR-0003: Base and template-specific JSON schemas

- Status: Accepted
- Date: 2026-07-24

## Decision

Validate every model against:

1. a versioned base DocxGen model schema;
2. a schema adjacent to the selected template.

The template schema records template id/version and hash metadata. `inspect`
cross-checks it against discovered placeholders. `validate-model` performs
schema and semantic validation without rendering.

## Rationale

Agents need exact required fields, value types, JSON paths, and remediation
hints. Placeholder inspection alone cannot determine whether a string is a
date, whether it may be empty, or the required shape of a collection item.

## Consequences

Template and schema changes are reviewed together. A stale template hash is a
template-contract error, not a warning hidden during render.
