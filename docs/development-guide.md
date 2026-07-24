# Development guide

## Prerequisites

- .NET 10 SDK compatible with `global.json`;
- Git;
- PowerShell 7 on Windows or POSIX shell on Linux/macOS;
- Word 2021/365 for final template acceptance;
- optional LibreOffice for visual regression rendering.

## Verify

Windows:

```powershell
./eng/verify.ps1
```

Linux/macOS:

```bash
./eng/verify.sh
```

## Packages

All versions belong in `Directory.Packages.props`. Project files contain
versionless `PackageReference` items. Commit generated `packages.lock.json`
files.

Before adding a dependency:

1. confirm no approved dependency already solves the problem;
2. verify current package and transitive licenses;
3. reject GPL, AGPL, LGPL, and proprietary licenses;
4. record the decision and update notices;
5. regenerate every affected `packages.lock.json`;
6. add every new exact package/version and reviewed SPDX expression to
   `eng/package-license-allowlist.json`;
7. run the full build and tests.

The license gate is offline and exact: it compares the union of all resolved
non-project dependencies in lock files with the allow-list. A package add,
remove, or version change therefore requires an explicit inventory update.
See `docs/package-license-policy.md`.

## Build policy

- warnings are errors;
- nullable is enabled;
- analyzers run during build;
- Release is the CI configuration;
- CI uses locked restore;
- generated artifacts go under ignored directories.

## Test policy

- unit tests are deterministic and independent;
- no remote network in rendering tests;
- fixtures are synthetic/anonymized;
- golden OOXML is normalized;
- visual changes require page-image review;
- CLI tests assert stdout, stderr, exit code, and filesystem side effects.

## Adding a public CLI option

Update in the same change:

- command definition;
- input DTO;
- Core request/options when applicable;
- JSON success and error contracts;
- CLI reference;
- agent workflow if behavior changes;
- end-to-end tests;
- changelog when user-visible.

## Adding a diagnostic

1. Use the correct category prefix from `docs/diagnostics.md`.
2. Add a stable constant to `DiagnosticCode`.
3. Add exactly one `DiagnosticDescriptor` to `DiagnosticRegistry`.
4. Supply a default message and concrete remediation hint.
5. Create the runtime value through `DiagnosticRegistry.Create` or
   `DiagnosticCollector.Add(code, ...)`.
6. Include an exact JSON Pointer or document path when possible.
7. Run the registry completeness tests and full verification.

Never reuse a code for different behavior or construct ad-hoc diagnostics when
a registered code exists.

## DOCX work

Do not inspect layout only through XML. Render representative DOCX output to
pages and inspect every page. Also run structural checks because visual export
does not validate every package relationship or field.
