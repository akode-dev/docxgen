# Branching and release

## Permanent branches

### `main`

- stable and releasable;
- protected after it is pushed;
- accepts release and hotfix pull requests;
- every release commit receives an annotated version tag.

### `develop`

- integration branch for Phase 1 development;
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

## Initial remote migration

The remote currently has `master`. The prepared local model uses `main` and
`develop`. When ready to publish:

```powershell
git push -u origin main
git push -u origin develop
```

Then change the default branch to `main` in GitHub, configure branch
protection, and only after verifying the new default consider deleting the
remote `master` branch:

```powershell
git push origin --delete master
```

Deleting `master` is intentionally not part of repository scaffolding.
