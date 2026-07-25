# Architecture decision records

ADRs record decisions that constrain future implementation.

| ADR | Status | Decision |
|---|---|---|
| [0001](0001-rendering-engine.md) | Superseded by ADR-0005 | DocxTemplater extensions |
| [0002](0002-template-placeholders.md) | Accepted | Text placeholders instead of content controls |
| [0003](0003-model-and-template-schema.md) | Accepted | Base and template-specific JSON schemas |
| [0004](0004-agent-first-cli-contract.md) | Accepted | JSON diagnostics and stable CLI contract |
| [0005](0005-hybrid-rendering-engine.md) | Accepted | Hybrid template binding and bounded Markdown renderer |
| [0006](0006-json-schema-validator.md) | Accepted | Pinned MIT JSON Schema validator |
| [0007](0007-semantic-docx-extraction.md) | Accepted | Bounded semantic DOCX-to-Markdown extraction |
| [0008](0008-template-json-schema-generation.md) | Accepted | Deterministic JSON Schema generation from DOCX markers |
| [0009](0009-public-product-and-package-naming.md) | Accepted | DocxGen product, Akode packages, and public naming |

An ADR is immutable after acceptance except for status and links to a
superseding ADR. New evidence is recorded in a new ADR.
