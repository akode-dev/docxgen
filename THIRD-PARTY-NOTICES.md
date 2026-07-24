# Third-party notices

The committed `packages.lock.json` files define the resolved dependency graph.
`eng/package-license-allowlist.json` records the reviewed license expression
for every exact package/version in that graph. The solution test suite fails
when either side changes without the other.

| Dependency | Planned version | License | Scope |
|---|---:|---|---|
| DocxTemplater | 2.8.3 | MIT | production |
| DocxTemplater.Markdown | 2.8.3 | MIT | P0 spike only |
| DocumentFormat.OpenXml | 3.5.1 | MIT | production |
| JsonSchema.Net | 8.0.5 | MIT | production |
| Markdig | 1.3.2 | BSD-2-Clause | production |
| System.CommandLine | 2.0.10 | MIT | production |
| Microsoft.Extensions.* | 10.0.0 | MIT | production |
| xUnit.net v3 | 3.2.2 | Apache-2.0 | tests |
| Shouldly | 4.3.0 | BSD-3-Clause | tests |

Only MIT, BSD-2-Clause, BSD-3-Clause, and Apache-2.0 dependencies are allowed.
The lock files and machine-checked allow-list, not this summary table, are
authoritative. Release packaging may generate a longer notice from the same
reviewed inventory.

## Rejected dependency

`DocxTemplater.Images` 2.8.3 is intentionally excluded. Although the extension
itself is MIT-licensed, it depends on `SixLabors.ImageSharp` 3.1.12 under the
Six Labors Split License, which is outside this repository's allow-list.

`DocxTemplater.Markdown` remains only in the retained P0 evidence project. It
is not referenced by a production project after ADR-0005.

JsonSchema.Net is intentionally pinned to 8.0.5, the last reviewed MIT-only
NuGet binary release before the 9.x maintenance-fee agreement. Version 9 and
later remain outside this repository's allow-list; see ADR-0006.
