# Diagnostic contract

Diagnostics are public API for people, CLI clients, CI, and coding agents.
Every emitted issue has:

```text
code + severity + message + hint + optional path
```

With `--json`, diagnostics are emitted in the
[versioned report envelope](report-format.md) defined by the
[report schema](schemas/docxgen-report-1.0.schema.json).

## Code format

```text
<severity>-<area>-<number>
```

Severity prefixes:

| Prefix | Meaning |
|---|---|
| `E` | operation-blocking error |
| `W` | recoverable warning |
| `I` | informational result |

Current areas:

| Area | Responsibility |
|---|---|
| `TPL` | template package, syntax, and inspection |
| `MDL` | JSON model and template-contract reconciliation |
| `SEC` | paths, assets, network, and resource policy |
| `MD` | Markdown preprocessing and supported semantics |
| `OUT` | generated document and field finalization |
| `MRG` | source merge and precedence |

Codes are never renumbered or reused for different behavior within a major
version.

## Registry

`DiagnosticCode` contains stable string constants. `DiagnosticRegistry` is the
authoritative metadata source for:

- default severity;
- default message;
- default actionable hint.

Use:

```csharp
var diagnostic = DiagnosticRegistry.Create(
    DiagnosticCode.ModelSchemaViolation,
    "/data/ds/Document/Title");
```

A pipeline stage can add the same diagnostic in order:

```csharp
diagnostics.Add(
    DiagnosticCode.AssetOutsideRoot,
    "/data/ds/ArchitectureDiagram");
```

Pass a context-specific message or hint only when it gives the caller more
precise repair information than the registered default. Empty messages and
hints are rejected.

## Paths

Model paths use JSON Pointer where possible:

```text
/data/ds/Document/Title
```

Template and document diagnostics may use a stable package part plus local
location. Paths must not expose temporary directories, credentials, or
confidential content.

## Completeness gate

`DiagnosticRegistryTests` reflects over every public constant in
`DiagnosticCode` and requires exactly one descriptor. It also checks:

- code format;
- prefix/severity agreement;
- unique registration;
- non-empty default messages;
- non-empty actionable hints.

The build must fail if a new code is added without complete metadata.
