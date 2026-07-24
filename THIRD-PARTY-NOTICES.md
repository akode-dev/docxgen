# Third-party notices

This file is an initial inventory and must be regenerated from the resolved
dependency graph in CI before the first binary release.

| Dependency | Planned version | License | Scope |
|---|---:|---|---|
| DocxTemplater | 2.8.3 | MIT | production |
| DocxTemplater.Markdown | 2.8.3 | MIT | P0 spike only |
| DocumentFormat.OpenXml | 3.5.1 | MIT | production |
| Markdig | 1.3.2 | BSD-2-Clause | production |
| System.CommandLine | 2.0.10 | MIT | production |
| Microsoft.Extensions.* | 10.0.0 | MIT | production |
| xUnit.net v3 | 3.2.2 | Apache-2.0 | tests |
| Shouldly | 4.3.0 | BSD-3-Clause | tests |

Only MIT, BSD-2-Clause, BSD-3-Clause, and Apache-2.0 dependencies are allowed.
The resolved transitive graph, not this table, is authoritative.

## Rejected dependency

`DocxTemplater.Images` 2.8.3 is intentionally excluded. Although the extension
itself is MIT-licensed, it depends on `SixLabors.ImageSharp` 3.1.12 under the
Six Labors Split License, which is outside this repository's allow-list.

`DocxTemplater.Markdown` remains only in the retained P0 evidence project. It
is not referenced by a production project after ADR-0005.
