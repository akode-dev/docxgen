# ADR-0005: Hybrid template binding and bounded Markdown rendering

- Status: Accepted
- Date: 2026-07-24
- Supersedes: ADR-0001

## Context

Evaluation showed that DocxTemplater 2.8.3 is useful for template schema discovery,
scalar/object replacement, collection row expansion, and package-part
preservation. Its Markdown extension does not satisfy the normative Phase 1
contract: it drops code, does not create hyperlinks, does not apply the Quote
style, and produces layout defects for soft line breaks and nested numbering.

`DocxTemplater.Images` is also unavailable because its transitive
SixLabors.ImageSharp dependency uses a license outside the project allow-list.

## Decision

Use a hybrid renderer:

1. Core parses Markdown with Markdig, applies security/heading policies, and
   produces a neutral, immutable Markdown block model.
2. DocxTemplater core binds scalar values, objects, collections, and
   conditions, and remains available for static template-schema inspection.
3. `Akode.DocxGen.Docx` owns a bounded Open XML renderer for the approved block
   model: paragraphs, H1-H6, emphasis, strikeout, inline/fenced code, links,
   block quotes, ordered/unordered lists, tables, local inline images,
   captions, and explicit breaks.
4. The public standalone marker remains `{{ds.Body}:MD}`. The Docx adapter owns
   that formatter name; it must not register `DocxTemplater.Markdown`.
5. Variable images are inserted directly with Open XML SDK. PNG/JPEG dimension
   parsing must use bounded header readers or another allow-listed dependency.
6. Styles, numbering definitions, widths, and colors remain template-owned.
7. Raw HTML and remote images remain disabled by default.

## Alternatives

### Keep the bundled extensions

Rejected. Critical Markdown semantics are lost, and the Images extension fails
the license policy.

### Convert Markdown with Pandoc or LibreOffice

Rejected for Phase 1. An external conversion process complicates packaging and
would require merging a separately generated document body back into the
template while preserving styles, sections, and relationships.

### Generate the entire DOCX directly

Rejected. It would duplicate template binding, collection, condition, and
package-preservation behavior already proven in DocxTemplater core.

## Consequences

- P2 must define the neutral Markdown block model and semantic conformance
  tests before P3 renders it.
- P3 scope grows by the bounded Open XML block renderer and image geometry.
- The renderer is intentionally not a general HTML/CSS or CommonMark engine.
- Every supported Markdown feature requires structural and visual fixtures.
- Rejected Markdown and image extensions remain absent from the dependency graph.
- The real approved Akode template still requires a P5 Word acceptance pass.
