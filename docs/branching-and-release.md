# Branching and release

## Permanent branches

### `main`

- stable and releasable;
- protected after it is pushed;
- accepts release and hotfix pull requests;
- every release commit receives an annotated version tag.

### `develop`

- integration branch for ongoing development;
- receives reviewed feature/fix pull requests;
- must remain buildable.

## Short-lived branches

- `feature/<short-name>` from `develop`;
- `fix/<short-name>` from `develop`;
- `release/<version>` from `develop` when packaging starts;
- `hotfix/<short-name>` from `main` after releases exist.

## Pull requests

Feature PRs target `develop`. They include:

- requirement/acceptance slice;
- concise design notes;
- verification commands and results;
- schema/ADR changes;
- binary fixture review notes when applicable.

## Releases

1. Create `release/<version>` from `develop`.
2. Freeze public contracts except release fixes.
3. Run full cross-platform, deterministic, license, package, and visual gates.
4. Update changelog and version metadata.
5. Merge into `main`.
6. Tag `v<version>`.
7. Merge release fixes back into `develop`.

The GitHub CI workflow runs for pull requests into `develop` or `main`, and
for pushes to `main`. This avoids paying for a duplicate matrix run when a
locally verified integration commit is pushed to `develop` and then released
to `main`.

The release workflow runs only for manual dispatch or a `v*` tag. Creating a
tag is therefore an explicit release and compute-cost decision.
