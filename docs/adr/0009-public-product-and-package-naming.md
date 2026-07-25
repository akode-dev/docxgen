# ADR-0009: Publish as DocxGen under the Akode namespace

- Status: Accepted
- Date: 2026-07-25

## Context

The original organization-specific name tied a reusable document engine to a
private identity. It also produced an executable whose product and package
names were harder to recognize in an AI-agent tool allow-list.

The repository is intended for three equal consumption modes:

- a cross-platform command-line executable called by people and agents;
- a ready-to-use .NET library embedded in another solution;
- technology-neutral Core contracts used with custom adapters.

The product already has a published `v1.0.0`, so changing namespaces, package
IDs, executable names, schema extension names, and agent-facing identifiers is
a breaking public-contract change.

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

Version 2.0.0 introduces this naming contract. No compatibility aliases retain the old
organization or shortened product name because the explicit requirement is to
remove those identities from source, binaries, templates, and documentation.

## Consequences

- AI agents can allow one recognizable `DocxGen` tool backed by the
  `docxgen` executable.
- NuGet consumers can choose a ready-to-run implementation or Core contracts
  without referencing the CLI.
- Windows, Linux, and macOS release archives have consistent names.
- Existing source consumers must update namespace and package references.
- Existing hooks, schemas, and anchored Markdown must update renamed public
  identifiers.
- Changelog, migration notes, and release notes must classify the change as
  breaking.
