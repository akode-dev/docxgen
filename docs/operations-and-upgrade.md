# Operations and upgrade guide

## Installation

From an internal NuGet feed:

```powershell
dotnet tool install --global Akode.DocxGen.Cli --version 1.0.0
docxgen --help
```

For an isolated agent workspace:

```powershell
dotnet tool install Akode.DocxGen.Cli `
  --tool-path .tools `
  --version 1.0.0 `
  --add-source <approved-feed>

.tools/docxgen --help
```

Self-contained release artifacts require no installed .NET runtime. Verify the
binary against its adjacent `SHA256SUMS` file before use.

## Runtime workflow

Use:

```text
inspect -> scaffold-model -> validate-model -> render --dry-run
        -> render --validate -> open in Word and refresh fields
```

Retain the JSON report with build logs. A warning `W-OUT-002` means the DOCX is
valid but Word must refresh TOC/PAGE/PAGEREF fields before delivery.

## Template upgrades

Treat a template as a versioned contract:

1. edit a copy in Word;
2. run `inspect --json`;
3. update its adjacent schema ID/version/hash and exact field requirements;
4. update the model and synthetic fixtures;
5. run short/long renders and Open XML validation;
6. refresh fields in desktop Word;
7. inspect every rendered page;
8. publish template and schema together.

Do not replace only the `.docx`: the stale `x-docxgen-templateHash` check will
fail deliberately.

## Tool upgrades

Before upgrading:

- read `CHANGELOG.md`;
- compare report/model schema versions;
- test representative templates in a disposable workspace;
- confirm package checksums and reviewed dependency notices;
- keep the previous tool version available for rollback.

Upgrade:

```powershell
dotnet tool update --global Akode.DocxGen.Cli --version <version>
```

Rollback:

```powershell
dotnet tool update --global Akode.DocxGen.Cli --version <previous-version>
```

## Failure handling

- exit `2`: correct CLI arguments;
- exit `3`: inspect/repair template or adjacent schema;
- exit `4`: follow diagnostic JSON Pointer paths in model/Markdown/assets;
- exit `5`: retry with `DOCXGEN_DEBUG=1` and retain the report;
- exit `6`: do not deliver the invalid package;
- exit `7`: check path, permissions, collision, or file lock;
- exit `1`: file a product defect with the debug report and minimal synthetic
  reproduction.

The renderer never requires an LLM credential and is offline by default.
Remote images should be mirrored locally for repeatable production builds.
