# ADR-0001: Rendering engine

- Status: Superseded by ADR-0005
- Date: 2026-07-24

## Context

Akode.DocxGen needs Markdown fragments, collections, conditions, and images
inside an existing DOCX template while preserving template styles and package
parts. Writing a complete Markdig-to-OOXML renderer would add significant
schedule and compatibility risk.

## Proposed decision

Use stable DocxTemplater 2.8.3 with its Markdown and Images extensions. Use
Open XML SDK 3.5.1 for inspection, deterministic post-processing, validation,
and normalized testing.

## Gate

This decision is not accepted until P0 proves behavior on a representative
branded template, including:

- style inheritance;
- heading offsets;
- nested list numbering;
- tables;
- local images;
- TOC/update fields;
- headers, footers, and text boxes;
- cover and final sections;
- Word and headless render compatibility.

## Consequences

Production renderer work waits for P0. If P0 fails, create a superseding ADR
that evaluates a different engine or a bounded custom renderer and re-estimates
the adapter phase.

## P0 outcome

The retained spike proved template schema extraction, scalar/object binding,
collection row expansion, fields, sections, headers, footers, VML text-box
replacement, and Open XML validation.

The proposed decision was rejected because:

- the Markdown extension omitted inline and fenced code content;
- links were emitted as text with no `w:hyperlink`;
- block quotes did not use the template `Quote` style;
- soft source-line breaks became hard Word breaks;
- nested ordered numbering had visible spacing defects;
- the Images extension introduced an unapproved ImageSharp license.

See `docs/spikes/p0-renderer-spike.md` for evidence and ADR-0005 for the
replacement decision.
