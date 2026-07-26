# Template authoring guide

This guide targets Word template designers and document-automation teams.

## Template package

Ship:

```text
template.docx
template.schema.json
```

The template contains design and fixed labels. The schema contains the exact
structural data contract. For a new or changed template, run:

```text
docxgen generate-schema --template template.docx --out template.schema.json --overwrite
docxgen generate-schema --template template.docx --out template.schema.json --check
```

The first command generates the contract; the second is suitable for CI.
Generated placeholder properties are optional. A marker tells DocxGen where a
value may be rendered; it does not tell DocxGen whether the business document
requires that value. Add selected paths to JSON Schema `required` arrays only
after deliberate review. Those explicit requirements remain mandatory even in
lenient mode. Use strict rendering when one run must populate every detected
placeholder.

## Optional document sections

1. Cover page.
2. Document Control page or a separate template variant.
3. Table of contents.
4. Main body marker.
5. Final branded page.

These sections demonstrate a common governed-document layout; none is an
engine requirement. A valid template may contain only one scalar, only
`{{ds.Body}:MD}`, several body slots, or nested data collections.

Use `Next Page` section breaks when headers, footers, or page setup change. Do
not use odd/even section starts unless a deliberate blank page is acceptable.

## Branding

Embed permanent brand elements directly:

- organization logo;
- cover/background artwork;
- theme colors and fonts;
- header/footer artwork;
- final-page contact details that do not vary.

Use placeholders only for variable information.

## Placeholders

Examples:

```text
{{ds.Document.Title}}
{{ds.Document.Author.FirstName}}
{{ds.Document.Author.LastName}}
{{ds.Document.Version}}
{{ds.Body}:MD}
```

Type each placeholder in one operation. Do not style individual characters.
Paste as plain text when copying. Run:

```text
docxgen inspect --template template.docx --include-text-probe --json
```

after every placeholder change.

`inspect --schema-out` emits an inspection DTO, not JSON Schema. Use
`generate-schema` for the adjacent `.schema.json`.

Markdown placeholders occupy an otherwise empty paragraph. Do not put a label,
punctuation, or other marker on the same paragraph.

## Required styles

- Normal;
- Heading 1 through Heading 6;
- List Paragraph;
- DocxGenTable or Table Grid;
- Quote;
- Code;
- CodeInline;
- Hyperlink;
- Caption.

Define explicit spacing, keep-with-next behavior, widow/orphan behavior,
numbering, table cell margins, and font fallbacks. Avoid relying on local Word
defaults.

## Main-body headings

For one `Body` slot:

```markdown
# Chapter
## Section
### Subsection
```

maps to Heading 1/2/3 with offset 0.

For a Markdown slot beneath a fixed template Heading 1, define and document the
expected positive offset. Do not mix the two conventions inside one template.

## TOC

Insert a real Word TOC field configured for the approved heading levels. The
renderer sets update-on-open. A new file may show stale page numbers until Word
opens and updates fields.

Do not replace the TOC with literal text or Markdown.

## Document Control

Use scalar placeholders for document metadata and a loop row for revision
history. A separate `with-document-control` template variant is preferred over
conditional deletion of an entire Word section.

## Images

Fixed artwork stays in the template. Variable Markdown images are inline,
aspect-preserving, and clamped to content width. Avoid floating text wrapping
for content figures.

For a variable standalone image, put `{{ds.ClientLogo}:IMG}` in an otherwise
empty paragraph and bind it with:

```json
{ "$file": "assets/client-logo.png" }
```

Optional formatter argument `alt=...` supplies alternative text, for example
`{{ds.ClientLogo}:IMG(alt=Client logo)}`. The image remains inline and is
clamped to the available content width.

## Acceptance

A template is accepted only after:

- `inspect` is clean;
- `generate-schema --check` is clean for generated contracts;
- schema and placeholder set agree;
- style-contract tests pass;
- short and long samples render;
- every rendered page is visually inspected;
- Word opens without repair;
- TOC and fields update;
- cover, final page, headers, footers, and text boxes remain correct.
