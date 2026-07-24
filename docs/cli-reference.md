# CLI reference

Phase 1 exposes six non-interactive commands. Use `docxgen <command> --help`
for the installed executable or:

```powershell
dotnet run --project src/Akode.DocxGen.Cli -- <command> --help
```

## Global conventions

- `--json` writes a report-contract `1.0` object to stdout.
- Human-oriented output is written to stderr.
- Success is exit code `0`; documented failures use stable codes `1`–`7`.
- Inputs are read before an output is atomically replaced.
- Existing outputs require `--overwrite` or `--force`, depending on command.
- Rendering defaults to strict, offline, and raw-HTML-disabled behavior.
- `DOCXGEN_DEBUG=1` includes exception details in handled failure diagnostics.

## `inspect`

Reads the template without generating a document:

```text
docxgen inspect
  -t|--template <template.docx>
  [--schema-out <inspection.json>]
  [--include-text-probe]
  [--json]
```

The result includes template ID/version/hash, placeholder paths/kinds,
collection item properties, requiredness from the adjacent schema, locations,
required Word styles, and inspection diagnostics.

## `scaffold-model`

Creates an editable model from inspected placeholders:

```text
docxgen scaffold-model
  -t|--template <template.docx>
  -o|--out <model.json>
  [--with-markdown-stubs]
  [--force]
  [--json]
```

With `--with-markdown-stubs`, Markdown placeholders become `$mdFile`
directives and the corresponding files are created below `sections/`.

## `validate-model`

Runs base schema, adjacent schema, template identity/hash, Markdown, asset,
merge, security, and placeholder-binding validation without producing DOCX:

```text
docxgen validate-model
  -t|--template <template.docx>
  -m|--model <model.json|->
  [--markdown <proposal.md>]
  [--assets-dir <directory>]
  [--lenient]
  [--json]
```

`-m -` reads UTF-8 JSON from stdin. In lenient mode, an absent optional
placeholder is removed with `W-MDL-007`; a schema-required field still fails.

## `render`

```text
docxgen render
  -t|--template <template.docx>
  -o|--out <output.docx>
  [-m|--model <model.json|->]
  [--markdown <proposal.md>]
  [--assets-dir <directory>]
  [--lenient]
  [--culture <ietf>]
  [--heading-offset <integer>]
  [--allow-raw-html]
  [--allow-remote-images]
  [--no-update-fields-on-open]
  [--set <path=value>]...
  [--doc-property <name=value>]...
  [--append-document-version]
  [--validate]
  [--dry-run]
  [--overwrite]
  [--json]
```

At least one of `--model` or `--markdown` is required. A standalone Markdown
input maps to `ds.Body`; section anchors map blocks to their named paths.

Model options supply defaults. A CLI option overrides only when explicitly
present. `--set` is highest-precedence model data and infers JSON booleans,
numbers, and null; prefix with `@` to force a string.

`--dry-run` executes the full preflight and does not write a DOCX.
`--validate` runs Open XML validation before atomic output.

`--append-document-version` reads `data.ds.Document.Version`, removes one
leading `v`, replaces unsafe filename characters with `-`, and inserts exactly
one `-v<version>` suffix before `.docx`.

## `convert`

Creates a quick standalone document without template placeholders:

```text
docxgen convert
  --markdown <input.md>
  -o|--out <output.docx>
  [--style-reference <reference.docx>]
  [--heading-offset <integer>]
  [--toc]
  [--validate]
  [--overwrite]
  [--json]
```

When `--style-reference` is absent, DocxGen creates a valid default Word style
set. With `--toc`, a real Word TOC field is inserted and marked for refresh.

## `validate`

```text
docxgen validate
  --file <document.docx>
  [--fail-on <error|warning>]
  [--max-errors <integer>]
  [--json]
```

The command opens the package with Open XML SDK and returns bounded validation
details.

## Exit codes

| Code | Meaning |
|---:|---|
| `0` | Success |
| `1` | Unexpected internal failure |
| `2` | Invalid usage |
| `3` | Template error |
| `4` | Model/Markdown/asset error |
| `5` | Rendering error |
| `6` | OOXML validation error |
| `7` | File-system/I/O error |

See [diagnostics](diagnostics.md) and
[machine-readable reports](report-format.md).
