# CLI reference

This is the planned Phase 1 public contract. The current executable only
registers command names as a scaffold.

## Global conventions

- `--json`: machine-readable stdout;
- logs: stderr;
- no prompts;
- `--verbosity quiet|minimal|normal|detailed|diagnostic`;
- safe defaults: strict, offline, no raw HTML;
- cancellation returns a documented non-success result without partial output.

## `inspect`

Reads a template contract. It does not require a model and does not generate a
DOCX.

```text
docxgen inspect -t <template.docx>
  [--schema-out <schema.json>]
  [--include-text-probe]
  [--json]
```

## `scaffold-model`

Creates an editable model skeleton and optional Markdown stubs.

```text
docxgen scaffold-model -t <template.docx> -o <model.json>
  [--with-markdown-stubs]
  [--force]
```

## `validate-model`

Hook-friendly validation of model, Markdown, and local assets without document
generation.

```text
docxgen validate-model -t <template.docx> -m <model.json>
  [--markdown <proposal.md>]
  [--assets-dir <directory>]
  [--strict | --lenient]
  [--json]
```

## `render`

```text
docxgen render -t <template.docx> -o <output.docx>
  [-m <model.json>]
  [--markdown <proposal.md>]
  [--assets-dir <directory>]
  [--strict | --lenient]
  [--culture <ietf>]
  [--heading-offset <integer>]
  [--allow-raw-html]
  [--allow-remote-images]
  [--update-fields-on-open | --no-update-fields-on-open]
  [--set <path=value>]...
  [--doc-property <name=value>]...
  [--append-document-version]
  [--validate]
  [--dry-run]
  [--overwrite]
  [--json]
```

For single-body templates use heading offset 0. For fragments inserted below a
fixed template heading, use the offset defined by that template contract.

## `convert`

```text
docxgen convert --markdown <input.md> --out <output.docx>
  [--style-reference <reference.docx>]
  [--heading-offset <integer>]
  [--toc]
  [--json]
```

## `validate`

```text
docxgen validate --file <document.docx>
  [--fail-on warning|error]
  [--max-errors <integer>]
  [--json]
```

## JSON failure

```json
{
  "ok": false,
  "exitCode": 4,
  "errorCode": "E-MDL-002",
  "message": "The model does not satisfy the template schema.",
  "errors": [
    {
      "path": "/data/ds/Document/Title",
      "rule": "minLength",
      "message": "Document title must not be empty.",
      "hint": "Set data.ds.Document.Title to a non-empty string."
    }
  ]
}
```

## Versioned output

`--append-document-version` inserts one normalized `v<version>` suffix before
the extension. It fails when the version field is absent. It never
auto-increments and never silently overwrites a collision.
