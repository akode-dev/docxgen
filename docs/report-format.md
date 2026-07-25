# Machine-readable report format

Every command writes the same versioned envelope to stdout when
`--json` is present. Human logs remain on stderr.

The normative Draft 2020-12 schema is
`schemas/docxgen-report-1.0.schema.json`.

## Envelope

| Field | Meaning |
|---|---|
| `reportVersion` | JSON contract version, currently `1.0` |
| `command` | `inspect`, `generate-schema`, `scaffold-model`, `validate-model`, `render`, `convert`, `extract`, or `validate` |
| `ok` | Whether the operation succeeded |
| `exitCode` | Numeric stable process exit code `0` through `7` |
| `errorCode` | Primary diagnostic code; present only on failure |
| `message` | Concise operation summary |
| `hint` | Primary remediation; present only on failure |
| `diagnostics` | Ordered information, warnings, and errors |
| `data` | Typed command-specific success data; absent on failure |

Each diagnostic has `code`, lower-case `severity`, `message`, `hint`, and an
optional exact `path`. The top-level `errorCode` and `hint` are copied from the
first error diagnostic so an agent can choose a repair without scanning human
logs.

## Success example

```json
{
  "reportVersion": "1.0",
  "command": "render",
  "ok": true,
  "exitCode": 0,
  "message": "Document rendered.",
  "diagnostics": [
    {
      "code": "W-OUT-002",
      "severity": "warning",
      "message": "Word fields require an update.",
      "hint": "Open the document in Word and update all fields before delivery."
    }
  ],
  "data": {
    "output": "out/Document-v1.0.docx",
    "outputBytes": 184320,
    "templateHash": "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
    "modelHash": "sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
    "durationMs": 412,
    "bound": [
      "ds.Document.Title",
      "ds.Body"
    ],
    "unbound": [],
    "markdownStats": {
      "sections": 7,
      "headings": 24,
      "tables": 3,
      "images": 2,
      "codeBlocks": 1
    },
    "validation": {
      "isValid": true,
      "errorCount": 0,
      "warningCount": 0
    },
    "dryRun": false,
    "documentVersion": "1.0"
  }
}
```

For `render --dry-run`, `data.dryRun` is true, `outputBytes` is zero, and no
output path is required.

Successful `extract` data contains `output`, `outputBytes`,
`assetsDirectory`, the absolute `assets` path array, `durationMs`, and
semantic `stats` counts for paragraphs, headings, list items, tables, and
image occurrences.

Successful `generate-schema` data contains `output`, `outputBytes`,
`templateId`, `templateVersion`, `templateHash`, `bindingCount`, `checked`,
and `durationMs`. `checked` is true only for a successful non-mutating
`--check`.

## Failure example

```json
{
  "reportVersion": "1.0",
  "command": "validate-model",
  "ok": false,
  "exitCode": 4,
  "errorCode": "E-MDL-002",
  "message": "Model validation failed.",
  "hint": "Update the reported value to satisfy the adjacent template schema.",
  "diagnostics": [
    {
      "code": "E-MDL-002",
      "severity": "error",
      "message": "The model violates the schema.",
      "hint": "Update the reported value to satisfy the adjacent template schema.",
      "path": "/data/ds/Document/Title"
    }
  ]
}
```

Failed JSON is still written to stdout before the process exits non-zero.
Agents should use `exitCode`, `errorCode`, `diagnostics[].path`, and `hint`.
They should not scrape stderr.

## Compatibility

- `reportVersion` is independent of tool, model, template, and document
  versions.
- Breaking removals, renames, type changes, or meaning changes require a new
  report major version.
- Additive optional fields may be introduced compatibly.
- Diagnostic codes and exit codes follow their own stable contracts.

Core owns the envelope and serializer. CLI handlers only project operation
results into the matching typed `data` record.
