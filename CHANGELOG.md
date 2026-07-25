# Changelog

All notable changes to DocxGen are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and Semantic
Versioning.

## [Unreleased]

## [2.0.0] - 2026-07-25

### Added

- Ready-to-use `Akode.DocxGen` runtime package and
  `DocxGenPipelineFactory.CreatePipeline()` for in-process .NET applications.
- Self-contained ARM64 release artifacts for Windows, Linux, and macOS, in
  addition to the existing x64 targets.
- Open-source contribution, security, support, conduct, issue, and pull-request
  guidance.
- `extract` command and Core/adapter contracts for semantic DOCX-to-Markdown
  conversion, including headings, inline formatting, links, native lists,
  quote/code/caption styles, GFM tables, and deterministic embedded-image
  export.
- Stable extraction diagnostics, resource limits, typed JSON report data, and
  report-contract `1.0` schema support for agent automation.
- `generate-schema` command for deterministic, self-contained Draft 2020-12
  contracts derived from placeholder-bearing DOCX templates, including nested
  objects/collections, conditions, headers/footers, `:MD`, and `:IMG`.
- Hierarchical template inspection and scaffold generation for arbitrary
  template topologies, plus non-mutating `generate-schema --check` drift
  detection and stable `SCH` diagnostics.

### Changed

- Established the former organization-specific product as **DocxGen**.
- Renamed .NET namespaces and projects to `Akode.DocxGen.*`, the dotnet tool
  package to `Akode.DocxGen.Tool`, and standalone executables to
  `docxgen`/`docxgen.exe`.
- Standardized machine-readable extension fields, Markdown section anchors,
  and the debug environment variable on the complete `docxgen` name.
- Moved the historical rendering experiment under `research/renderer-spike`
  and removed it from the production solution.
- Reworked public documentation and package/release metadata for CLI,
  AI-agent, and embedded library consumers.

### Breaking

- The product identity changes public namespaces, package IDs, executable names,
  `x-docxgen-*` schema extension fields, `docxgen:section` anchors, and the
  `DOCXGEN_DEBUG` environment variable. This release is therefore 2.0.0.

## [1.0.0] - 2026-07-24

### Added

- Cross-platform .NET 10 CLI commands: `inspect`, `scaffold-model`,
  `validate-model`, `render`, `convert`, and `validate`.
- Deterministic JSON model envelope with base and adjacent template schemas,
  template identity/hash checks, recursive values, collections, and `$md`,
  `$mdFile`, `$file`, and `$text` directives.
- Section-anchored Markdown and table-to-collection conversion.
- Bounded Markdig/Open XML renderer for headings, paragraphs, formatting,
  links, native lists, tables, quotes, code, horizontal rules, and local/remote
  images.
- Strict/lenient binding, model/CLI option precedence, atomic output,
  document properties, field refresh, leftover detection, OOXML validation,
  and optional business-version filename suffixes.
- Stable diagnostic codes, exit codes, and report-contract `1.0` JSON output
  designed for coding agents and validation hooks.
- Synthetic proposal template with cover, Document Control, revision loop,
  TOC, Markdown body, closing page, adjacent schema, and runnable sample.
- 80 automated tests plus Microsoft Word/PDF visual acceptance evidence.
- Locked dependencies, reviewed permissive-license gate, third-party notices,
  cross-platform CI, dotnet tool package, and self-contained single-file
  release workflow for Windows, Linux, and macOS x64.

### Security

- Offline rendering by default.
- Local asset root containment and resource limits.
- Opt-in remote image downloads with timeout, size/media-type limits, redirects
  disabled, and non-public address rejection.

[Unreleased]: https://github.com/akode-dev/docxgen/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/akode-dev/docxgen/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/akode-dev/docxgen/releases/tag/v1.0.0
