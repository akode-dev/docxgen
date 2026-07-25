# Contributing to DocxGen

Thank you for helping make document automation easier to reuse. Contributions
may include bug reports, new template scenarios, tests, documentation, and
focused implementation changes.

## Before opening an issue

- Search existing issues and discussions.
- Remove confidential content from DOCX files, JSON, screenshots, and logs.
- Reduce a document problem to the smallest synthetic template that reproduces
  it.
- Include the DocxGen version, operating system, command, exit code, and
  machine-readable diagnostics when available.

Security vulnerabilities must follow [SECURITY.md](SECURITY.md), not a public
issue.

## Development setup

DocxGen requires the .NET SDK selected by `global.json`.

```shell
git clone https://github.com/akode-dev/docxgen.git
cd docxgen
dotnet restore DocxGen.sln
dotnet build DocxGen.sln --configuration Release --no-restore
dotnet test DocxGen.sln --configuration Release --no-build
```

On Windows, `./eng/verify.ps1` runs the repository verification flow. On
Linux/macOS, use `./eng/verify.sh`.

## Branches and pull requests

The repository uses `main` for releases and `develop` for integration. Create
`feature/<short-name>` or `fix/<short-name>` from `develop`, and target
`develop` with the pull request.

A good pull request:

- solves one bounded problem;
- preserves the architecture boundaries in `AGENTS.md`;
- includes tests for changed behavior;
- updates public documentation and `CHANGELOG.md` when needed;
- records any public contract decision in an ADR;
- explains the commands actually used for verification.

Binary DOCX fixtures require both structural validation and a page-by-page
visual review. Never contribute customer documents, credentials, personal
data, or licensed corporate templates without redistribution rights.

## Code and dependency policy

- Target .NET 10 and C# 14.
- Keep nullable analysis and warnings-as-errors enabled.
- Use xUnit v3 and Shouldly in tests.
- Keep package versions in `Directory.Packages.props`.
- Commit affected `packages.lock.json` files.
- Add only dependencies accepted by the repository license policy.

Read the [development guide](docs/development-guide.md),
[architecture](docs/architecture.md), and
[template authoring guide](docs/template-authoring-guide.md) before changing
their respective areas.

By participating, you agree to the [Code of Conduct](CODE_OF_CONDUCT.md) and
that your contribution is licensed under the project's MIT License.
