# ADR-0009: Public product and package naming

- Status: Accepted
- Date: 2026-07-25

## Context

The repository serves three equal consumption modes:

- a cross-platform command-line executable called by people and agents;
- a ready-to-use .NET library embedded in another solution;
- technology-neutral Core contracts used with custom adapters.

These modes need one recognizable product name and deterministic identifiers
across command lines, .NET APIs, schemas, release archives, and AI-agent tool
allow-lists.

## Decision

Use the following public naming:

| Surface | Name |
|---|---|
| Product | `DocxGen` |
| Executable and command | `docxgen` (`docxgen.exe` on Windows) |
| Friendly agent tool name | `DocxGen` |
| Ready-to-use NuGet package | `Akode.DocxGen` |
| Core contracts package | `Akode.DocxGen.Core` |
| .NET tool package | `Akode.DocxGen.Tool` |
| .NET namespaces/projects | `Akode.DocxGen.*` |
| Schema extension prefix | `x-docxgen-*` |
| Markdown section marker | `docxgen:section` |
| Debug environment variable | `DOCXGEN_DEBUG` |

The production solution is `DocxGen.sln`. Historical experiments live below
`research/` and are excluded from the production solution and release build.
GitHub release assets use lowercase `docxgen-<rid>` names for predictable
automation.

Version 2.0.0 is the first public release governed by this naming contract.

## Consequences

- AI agents can allow one recognizable `DocxGen` tool backed by the
  `docxgen` executable.
- NuGet consumers can choose a ready-to-run implementation or Core contracts
  without referencing the CLI.
- Windows, Linux, and macOS release archives have consistent names.
- Hooks, schemas, anchored Markdown, packages, and executables share the same
  stable identifiers.
- Future changes to these names require a new ADR and semantic-version review.
