# Model format

## Envelope

```json
{
  "$schema": "./docs/schemas/docxgen-model-1.0.schema.json",
  "modelVersion": "1.0",
  "template": {
    "id": "akode-proposal",
    "version": "1.0.0"
  },
  "options": {
    "culture": "en-US",
    "strict": true,
    "headingOffset": 0
  },
  "data": {
    "ds": {}
  }
}
```

`modelVersion` identifies this envelope. `template.version` identifies the
visual/data contract. Neither is the business document version.

## Base reader contract

`ModelJsonReader` implements the first validation layer in Core:

1. read the input as UTF-8, accepting an optional BOM;
2. compute `sha256:<lowercase-hex>` over the exact source bytes;
3. reject malformed JSON and duplicate object properties;
4. reject unknown `$...` directives below `data`;
5. validate the embedded Draft 2020-12 base schema;
6. materialize an immutable recursive `ModelDocument`.

Failures return registered diagnostics instead of a partial model. Diagnostic
paths use JSON Pointer syntax, for example `/data/ds/Body/$html`. The embedded
schema is the same checked-in
`docs/schemas/docxgen-model-1.0.schema.json`, so validation does not depend on
the process working directory.

When `options` is absent, Core applies the safe defaults: culture `en-US`,
strict mode enabled, heading offset `0`, raw HTML and remote images disabled,
and Word field updates enabled.

The base reader intentionally does not open `$mdFile` or `$file` paths.
The pipeline then applies path containment, resource limits, asset resolution,
and the adjacent template-specific schema.

Model options are defaults. An explicitly present CLI option wins; omitted CLI
options preserve the model value.

## Recommended proposal model

```json
{
  "$schema": "./docs/schemas/docxgen-model-1.0.schema.json",
  "modelVersion": "1.0",
  "template": {
    "id": "akode-proposal",
    "version": "1.0.0"
  },
  "options": {
    "culture": "en-US",
    "strict": true,
    "headingOffset": 0
  },
  "data": {
    "ds": {
      "Document": {
        "Title": "Customer Platform Proposal",
        "Description": "Technical and commercial proposal",
        "Project": "Customer Platform Modernization",
        "Client": "Example Corporation",
        "Version": "1.0",
        "Status": "Draft",
        "Date": "2026-07-24",
        "Classification": "INTERNAL",
        "Author": {
          "FirstName": "Sample",
          "LastName": "Author",
          "Role": "Solution Architect",
          "Email": "sample.author@example.test"
        }
      },
      "Revisions": [
        {
          "Version": "1.0",
          "Date": "2026-07-24",
          "Author": "Sample Author",
          "Description": "Initial version"
        }
      ],
      "Body": {
        "$mdFile": "proposal.md"
      }
    }
  }
}
```

## Directives

### `$mdFile`

```json
{ "$mdFile": "sections/approach.md" }
```

The path is relative to `--assets-dir` or the model directory. It must remain
inside the approved root.

### `$md`

```json
{ "$md": "A **short** Markdown fragment." }
```

Use only for short generated fragments. Files are preferred for long sections.

### `$file`

```json
{ "$file": "assets/client-logo.png" }
```

Use it with a binary placeholder such as
`{{ds.ClientLogo}:IMG(alt=Client logo)}`. Phase 1 supports the same image media
types as Markdown images.

### `$text`

```json
{ "$text": "Literal * characters are not Markdown." }
```

## Collections

JSON arrays bind to template loops. Items should be objects with stable
property names. Empty collections are valid only when the template schema
allows them.

The base schema uses Draft 2020-12 dynamic recursion, so arrays and ordinary
objects may contain further arrays, objects, scalar values, or supported
directives at any depth. Practical depth and aggregate-size limits are
enforced later by the security layer, not by a fixed schema nesting level.

## Section-anchored Markdown

For a proposal that is easier to edit as one file, use standalone HTML
comments to bind Markdown blocks to model paths:

```markdown
<!-- docxgen:section ExecutiveSummary -->

Executive summary content.

<!-- docxgen:section ds.Approach -->

## Delivery approach

<!-- docxgen:section Team format=table columns=Name,Role -->

| Name | Role |
|---|---|
| Alexei | Solution Architect |

<!-- docxgen:end -->
```

Unqualified names resolve below `ds`; dotted names are absolute. Names and
result paths are case-sensitive. The marker keyword is case-insensitive.
Content continues until the next section marker, `docxgen:end`, or EOF.

The parser deliberately:

- ignores marker-shaped text inside fenced code and inline code;
- accepts UTF-8 BOM, CRLF, and trailing marker whitespace;
- rejects duplicate and parent/child-overlapping paths instead of applying
  last-writer-wins;
- warns when nonblank content appears before the first marker;
- preserves source order and normalizes returned blocks to LF.

For `format=table`, `columns` supplies the object property names. The one GFM
pipe table in the block becomes a collection of plain-text objects suitable
for a template loop.

Source precedence is deterministic:

```text
--set > model.json > anchored Markdown
```

JSON and Markdown are deep-merged, so model metadata can coexist with Markdown
body sections. An explicit JSON value at the same leaf wins and produces
`W-MRG-001`. `--set` values infer number, boolean, and null types; prefix with
`@` to force a string such as `@0042`.

## Null and empty values

- `null` never satisfies a required field;
- whitespace-only strings fail `minLength` after product-level trimming;
- lenient mode may remove optional unbound placeholders, but it never makes an
  invalid required schema valid;
- strict mode is the default.

## Template-specific schema

The base schema cannot know whether `Document.Title` exists. Each governed
template therefore ships its own schema. Generate its structural baseline:

```text
docxgen generate-schema --template template.docx --out template.schema.json
```

The generated schema is self-contained, closes discovered objects, requires
all statically reachable bindings, models nested collections, constrains
template identity/version, and records the template hash. `inspect`
cross-checks the adjacent schema with discovered placeholders.

Schema generation is deliberately syntax-driven. Dates, emails, enums,
descriptions, defaults, numeric limits, and business optionality are not
guessed from labels or document appearance.

## Agent behavior

Agents should:

1. run `inspect`;
2. run `generate-schema` for a new template or `generate-schema --check` for
   a governed one;
3. generate or update the scaffold;
4. edit model and Markdown;
5. run `validate-model`;
6. fix each error using `path` and `hint`;
7. run `render --dry-run`;
8. render the final file.

Agents must not switch to lenient mode to hide missing required content.
