# NuGet.org publishing

DocxGen publishes three public packages:

| Package | Purpose |
|---|---|
| `Akode.DocxGen` | Ready-to-use DOCX pipeline |
| `Akode.DocxGen.Core` | Technology-neutral contracts and orchestration |
| `Akode.DocxGen.Tool` | Cross-platform `docxgen` .NET tool |

The release workflow uses NuGet.org trusted publishing. GitHub exchanges an
OIDC token for a short-lived NuGet API key at release time, so the repository
does not store a long-lived publishing key.

## One-time maintainer setup

1. Sign in to [NuGet.org](https://www.nuget.org/) and open
   **Trusted Publishing** from the account menu.
2. Create a GitHub Actions policy. Select the NuGet.org user or organization
   that should own the packages, then enter:

   | Field | Value |
   |---|---|
   | Repository owner | `akode-dev` |
   | Repository | `docxgen` |
   | Workflow file | `release.yml` |
   | Environment | leave empty |

3. In GitHub, open **Settings → Secrets and variables → Actions → Secrets** and
   create `NUGET_USER`. Its value is the NuGet.org profile name that owns the
   publishing identity, not an email address.
4. Under **Variables**, create `NUGET_PUBLISH_ENABLED` with value `true`.

The package IDs must be available or already owned by the same NuGet.org
account. NuGet package versions are immutable after publication.

## First publication

After the policy, secret, and variable exist, run the `release` workflow
manually from the `main` branch. A manual run packs the version declared in
`Directory.Build.props` and publishes it to NuGet.org without rebuilding the
self-contained GitHub Release archives.

```shell
gh workflow run release.yml --repo akode-dev/docxgen --ref main
```

The package IDs were first created with `2.1.0`. The workflow
expects exactly three `.nupkg` files and uses `--skip-duplicate`, so rerunning a
completed publication does not overwrite an immutable package.

## Later releases

For subsequent versions:

1. update `Version` and `PackageVersion` in `Directory.Build.props`;
2. update `CHANGELOG.md`;
3. run the complete release verification;
4. merge the release into `main`;
5. push the matching `v<version>` tag.

When `NUGET_PUBLISH_ENABLED` is `true`, a version tag publishes NuGet packages
and the cross-platform GitHub Release in the same workflow. NuGet.org performs
additional validation and indexing after upload, so search availability may
lag behind a successful workflow.

## Disabling publication

Set `NUGET_PUBLISH_ENABLED` to `false` or remove the repository variable.
GitHub Releases continue to work, but the NuGet publishing job is skipped.
Revoke the policy in NuGet.org if this repository should no longer be trusted.

Official references:

- [Trusted publishing on NuGet.org](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
- [Publishing packages to NuGet.org](https://learn.microsoft.com/nuget/nuget-org/publish-a-package)
