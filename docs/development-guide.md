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
5. regenerate lock files;
6. run the full build and tests.

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

- use the correct category prefix;
- add a stable constant;
- add default message and actionable hint;
- include a path where possible;
- cover it with a test;
- do not reuse a code for different behavior.

## DOCX work

Do not inspect layout only through XML. Render representative DOCX output to
pages and inspect every page. Also run structural checks because visual export
does not validate every package relationship or field.
