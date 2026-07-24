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

`ModelJsonReader` now implements the first validation layer in Core:

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
Path containment, limits, asset resolution, and the adjacent
template-specific schema are subsequent P2 stages.

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
        "ClientName": "Example Corporation",
        "Date": "2026-07-24",
        "Version": "1.0",
        "Status": "Draft",
        "Author": {
          "FirstName": "Andrei",
          "LastName": "Ivanov"
        }
      },
      "DocumentControl": {
        "Owner": "Akode",
        "Classification": "Confidential",
        "Revisions": [
          {
            "Version": "1.0",
            "Date": "2026-07-24",
            "Author": "Andrei Ivanov",
            "Description": "Initial version"
          }
        ]
      },
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

The template formatter decides how the binary value is used.

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

## Null and empty values

- `null` never satisfies a required field;
- whitespace-only strings fail `minLength` after product-level trimming;
- lenient mode may remove optional unbound placeholders, but it never makes an
  invalid required schema valid;
- strict mode is the default.

## Template-specific schema

The base schema cannot know whether `Document.Title` is required. Each template
therefore ships its own schema with exact requirements. `inspect` cross-checks
the schema with discovered placeholders.

## Agent behavior

Agents should:

1. run `inspect`;
2. generate or update the scaffold;
3. edit model and Markdown;
4. run `validate-model`;
5. fix each error using `path` and `hint`;
6. run `render --dry-run`;
7. render the final file.

Agents must not switch to lenient mode to hide missing required content.
