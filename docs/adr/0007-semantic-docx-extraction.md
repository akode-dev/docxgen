# ADR-0007: Semantic DOCX-to-Markdown extraction

- Status: Accepted
- Date: 2026-07-25

## Context

Agents often receive an existing Word document that must become editable,
diffable source before it can re-enter the DocxGen workflow. DOCX contains
layout, package, field, and revision concepts that have no portable Markdown
equivalent. Promising a lossless round trip would make output unpredictable
and couple Core to Open XML.

## Decision

Add a technology-neutral `IDocxMarkdownExtractor` contract to Core and
implement it in the DOCX adapter with Open XML SDK. Expose it as:

```text
docxgen extract --file input.docx --out output.md
```

The initial contract reads the main document body and maps supported semantics:
Heading 1–6, paragraphs, bold/italic/strikethrough, inline code, hyperlinks,
hard line breaks, ordered/unordered nested lists, quote/code/caption styles,
horizontal rules, GFM tables, and embedded images.

Images are copied without transcoding, named deterministically, and linked
relative to the Markdown output. Generated fields and unsupported constructs
produce stable diagnostics. Macro-enabled packages and configured resource
limit violations fail extraction.

The command does not claim byte-for-byte, style, layout, tracked-change, or
template-placeholder round-trip fidelity.

## Consequences

- Agents get a deterministic text-and-assets bundle without Office automation.
- Core remains independent of Open XML and file-system writes.
- The CLI owns path resolution, overwrite preflight, atomic per-file writes,
  and JSON report projection.
- Headers, footers, comments, footnotes, floating layout, complex merged
  tables, fields, and tracked deletions are omitted or downgraded in the
  initial version.
- Future additions must remain bounded and receive tests plus an ADR when they
  materially change fidelity or security.
