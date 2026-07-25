# Akode.DocxGen agent instructions

These instructions apply to the entire repository. More specific `AGENTS.md`
files may be added later for subtrees when genuinely different rules are
needed.

## Start here

1. Read `README.md`.
2. Read `docs/technical-specification.md` and `docs/architecture.md`.
3. Read `docs/implementation-plan.md`, then follow the user/issue scope. Phase
   2 work requires an explicitly selected backlog item.
4. Inspect the working tree before editing. Preserve unrelated user changes.

Phase 1 is released; DOCX extraction and template-schema generation are
implemented for the next minor release. Preserve the eight-command CLI,
model/report schemas, stable diagnostics, security defaults, deterministic
template-schema generation, template contract, and cross-platform packaging
behavior. New product scope belongs to the Phase 2 backlog unless the user or
issue explicitly selects it.

## Architecture boundaries

- `Akode.DocxGen.Core` owns contracts, model parsing, Markdown preprocessing,
  diagnostics, security policy, and pipeline orchestration.
- `Akode.DocxGen.Docx` is the DOCX adapter and may reference DocxTemplater and
  Open XML SDK.
- `Akode.DocxGen.Cli` is a thin command-line adapter. Business logic does not
  belong in command handlers.
- `Akode.DocxGen.Mcp` is Phase 2. Do not duplicate Core or CLI behavior there.
- Core must never reference Docx, CLI, MCP, or Open XML implementation types.
- No project may reference CLI.
- Console output belongs only in `Akode.DocxGen.Cli/Output`.

## Product rules

- Content lives in Markdown/JSON; design lives in the DOCX template.
- ADR-0001 is superseded by ADR-0005. Production keeps DocxTemplater only for
  template binding/schema behavior and implements the approved, bounded
  Markdown block renderer with Markdig and Open XML SDK.
- Do not restore `DocxTemplater.Markdown`, `DocxTemplater.Images`, ImageSharp,
  or another document engine without a new ADR and license review.
- Treat `inspect`, `generate-schema`, JSON output, diagnostics, exit codes,
  and `hint` fields as public API.
- A fixed corporate logo remains in the template. Variable content figures
  normally arrive through Markdown.
- Rendering is offline by default. Remote images and raw HTML remain disabled
  unless explicitly enabled by the caller.
- DOCX extraction is semantic, reads the main body, and exports embedded
  images. It does not promise a layout or tracked-change round trip.
- Never weaken strict mode just to make a failing fixture pass.
- Do not edit binary `.docx` fixtures without an explicit fixture task and a
  documented visual/structural verification step.

## Required verification

Run from the repository root:

```powershell
dotnet restore Akode.DocxGen.sln
dotnet build Akode.DocxGen.sln --configuration Release --no-restore
dotnet test Akode.DocxGen.sln --configuration Release --no-build
```

For package changes, regenerate and commit lock files, review every new
resolved package/version, and update `eng/package-license-allowlist.json`.
Architecture and license gates run as part of the solution tests. For template
or rendering changes, run the relevant golden tests and inspect rendered pages
according to `docs/template-authoring-guide.md`.

## Coding standards

- Target .NET 10 and C# 14.
- Keep nullable reference types enabled and warnings as errors.
- Centralize every package version in `Directory.Packages.props`.
- Use asynchronous APIs for I/O and accept `CancellationToken`.
- Use stable diagnostic codes. Every user-actionable error needs a concrete
  remediation `Hint`.
- Prefer immutable records for requests, results, schemas, and diagnostics.
- Tests use xUnit v3 and Shouldly. Do not add FluentAssertions.
- Do not add packages with licenses outside the repository allow-list.

## Git

- `main` is stable and releasable.
- `develop` is the integration branch.
- Create `feature/<short-name>` or `fix/<short-name>` from `develop`.
- Do not push, force-push, rewrite shared history, or create releases unless
  the user explicitly asks.
- Keep commits focused and use imperative commit subjects.

## Agent handoff

Before ending an implementation task, report:

- changed behavior and public contracts;
- tests and validation actually run;
- remaining TODOs or unverified assumptions;
- any ADR or schema changes;
- the current branch and whether changes are committed.
