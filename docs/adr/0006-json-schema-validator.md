# ADR-0006: JSON Schema validation dependency

- Status: Accepted
- Date: 2026-07-24

## Context

The base model and adjacent template contracts use JSON Schema Draft 2020-12.
Implementing that standard inside DocxGen would create a second, incomplete
schema engine and make diagnostics drift from the checked-in schemas.

JsonSchema.Net 9.x binary releases include an additional maintenance-fee
agreement for revenue-generating users. That binary agreement is outside this
repository's permissive MIT/BSD/Apache-2.0 policy even though the source
repository remains MIT.

Corvus.Json.Validator is Apache-2.0 and supports Draft 2020-12, but its dynamic
validator pulls the code-generation stack and performs runtime code
generation. That graph and cold-start behavior are disproportionate for an
offline CLI whose model schema is small and whose package-size target is
strict.

## Decision

Use `JsonSchema.Net` 8.0.5, the last reviewed NuGet binary release before 9.x
whose package metadata is the plain MIT expression. It includes the 8.x
reference-resolution fixes and supports .NET 10 consumers. Lock its transitive
graph and validate the license inventory in CI.

The adapter remains behind DocxGen-owned model validation types. Application
code must not expose JsonSchema.Net result types in public contracts.

## Consequences

- Core gets standards-based Draft 2020-12 validation without a custom schema
  interpreter.
- Package upgrades are intentionally blocked by the exact lock files and
  license allow-list.
- Do not upgrade JsonSchema.Net above 8.0.5 merely to obtain a newer version.
  A future upgrade requires a new dependency/license review and either an
  approved binary distribution or a replacement validator.
- Vulnerability audit and the resolved-license gate remain required for every
  package change.
