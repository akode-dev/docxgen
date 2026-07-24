# Package-license policy

Akode.DocxGen accepts only dependencies reviewed under these SPDX license
expressions:

- `MIT`;
- `BSD-2-Clause`;
- `BSD-3-Clause`;
- `Apache-2.0`.

This is a repository policy, not an automatic legal conclusion. A maintainer
must review a dependency before adding it.

## Authoritative inputs

The gate compares two committed inputs:

1. every `packages.lock.json` supplies the exact resolved direct and transitive
   package graph for its project;
2. `eng/package-license-allowlist.json` records one approved license
   expression for every distinct package id and resolved version.

The comparison is exact and case-insensitive for package identities. A package
addition, removal, or version change fails the solution tests until the
allow-list is deliberately reconciled. Project references in lock files are
not third-party packages and are ignored.

`eng/package-license-allowlist.schema.json` documents the inventory shape.
`THIRD-PARTY-NOTICES.md` remains the human-readable summary; the lock files and
allow-list are the machine-checked sources.

## Enforced exceptions

- `DocxTemplater.Markdown` is allowed only in the retained P0 spike lock file.
- `DocxTemplater.Images` and `SixLabors.ImageSharp` are forbidden throughout
  the resolved graph.
- DocxTemplater and Open XML SDK production references are confined to
  `Akode.DocxGen.Docx`.

The package-scope checks and project-reference rules live in
`Akode.DocxGen.Core.Tests` so every normal solution test run enforces them
offline on Windows, Linux, and macOS.

## Updating a dependency

1. Change the centrally managed version in `Directory.Packages.props`.
2. Regenerate and review all affected `packages.lock.json` files.
3. Inspect the package metadata and license files for every new transitive
   package/version.
4. Reject the change or record its exact SPDX expression in
   `eng/package-license-allowlist.json`.
5. Remove stale inventory entries and update `THIRD-PARTY-NOTICES.md` when the
   human summary changes.
6. Run `dotnet restore Akode.DocxGen.sln --locked-mode` and `eng/verify`.

Never add an allow-list entry solely to silence the test. License changes,
compound expressions, custom terms, or missing metadata require an explicit
review and, when they affect the architecture decision, a new ADR.
