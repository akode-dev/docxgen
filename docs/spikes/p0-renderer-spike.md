# P0 renderer spike results

- Date: 2026-07-24
- Branch: `feature/p0-renderer-spike`
- Outcome: ADR-0001 rejected; ADR-0005 accepted
- Fixture: synthetic, non-confidential proposal template

## Reproduce

The harness and fixtures are in
`research/renderer-spike`. Its README contains the build and run
commands. The template builder creates:

1. cover with scalar fields and a VML text-box placeholder;
2. Document Control metadata and a repeated revision row;
3. a real TOC field;
4. a standalone `{{ds.Body}:MD}` paragraph;
5. body/header/footer placeholders;
6. a final next-page section.

## Verified behavior

| Capability | Result | Evidence |
|---|---|---|
| Actual API | Pass | `MarkdownFormatter(MarkDownFormatterConfiguration)` and `{{ds.Body}:MD}` |
| Template schema | Pass | object `Document`, collection `Revisions`, scalar `Body` |
| Scalar/object binding | Pass | body, header, footer, and VML text box |
| Collection rows | Pass | three revision rows, no marker row remains |
| Headings H1-H4 | Pass | Word heading styles and updated TOC entries |
| Bold/italic | Pass | run properties retained |
| Nested lists | Partial | semantic nesting works; nested ordered labels miss spacing |
| Pipe table | Pass | `DocxGenTable` is applied |
| Local image | Pass | one inline SVG inserted by Open XML post-processing |
| Final section | Pass | final page remains last |
| Fields | Pass | `w:updateFields`, Word updated one TOC and page fields |
| Package validity | Pass | OpenXmlValidator for Microsoft 365 reports zero errors |
| Unresolved tokens | Pass | no `{{...}}` remains in XML parts |
| Word rendering | Pass | Word opened without repair and exported seven pages |

## Failed gate items

The full Markdown fixture contains a block quote, inline code, fenced C# code,
a link, raw HTML, and source-wrapped paragraphs. OOXML inspection and visual
rendering showed:

- inline code text was omitted;
- fenced code content was omitted;
- link text remained, but no `w:hyperlink` was created;
- block-quote text remained, but no `Quote` paragraph style was applied;
- raw HTML was stripped, which matches the default security policy;
- source soft line breaks became `w:br`, causing excessive justification
  spacing until the fixture paragraphs were unwrapped;
- nested ordered numbering rendered labels such as `a.` without adequate
  following spacing.

These are semantic failures, not template color or spacing choices. A
different corporate template cannot repair missing OOXML content.

## Image-extension license result

`DocxTemplater.Images` 2.8.3 depends on `SixLabors.ImageSharp` 3.1.12. The
[official ImageSharp repository](https://github.com/SixLabors/ImageSharp)
identifies the Six Labors Split License, which is outside this project's
MIT/BSD/Apache allow-list. The [NuGet dependency graph](https://www.nuget.org/packages/DocxTemplater.Images/2.8.3)
confirms the transitive package. The extension was removed from every
production project and lock files.

## Visual QA

The packaged `render_docx.py` workflow was attempted first, but this Windows
environment has no LibreOffice executable. The fallback used installed
Microsoft Word invisibly for field update and PDF export, followed by Poppler
rasterization. All seven pages were inspected:

- cover fits on one page;
- Document Control and revision table fit on one page;
- TOC hierarchy and page numbers are correct;
- body headings, tables, lists, figure, headers, and footers are legible;
- final branded page remains last.

Word automation was used only for local QA. It is not a runtime dependency and
remains a product non-goal.

## Decision

Keep DocxTemplater core for template binding and static schema behavior. Reject
its Markdown and Images extensions for production. Implement the normative
Markdown subset through the bounded Markdig/Open XML design in ADR-0005.

The approved real Akode template was not available. P5 still requires a
separate Word acceptance pass with that template, but it cannot change the P0
semantic failures or license decision.
