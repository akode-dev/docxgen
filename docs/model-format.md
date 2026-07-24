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
