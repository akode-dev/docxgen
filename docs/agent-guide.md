# Coding-agent guide

This repository is designed for Codex CLI/app/cloud and Claude Code. Shared
project rules live in root `AGENTS.md`; Claude-specific loading starts in root
`CLAUDE.md`.

## Start a task

1. Read the root agent file loaded by your product.
2. Run `git status --short --branch`.
3. Read the relevant specification and ADR.
4. Confirm the task belongs to the current implementation phase.
5. Create or switch to a short-lived branch from `develop` only when the user
   or workflow authorizes branch creation.
6. Make the smallest vertical change that satisfies one acceptance slice.
7. Run focused tests, then `eng/verify`.
8. Update documentation/schema/ADR when a public contract changes.

## Current product state

Phase 1 is complete. The eight-command CLI, Core pipeline, DOCX adapter,
reference template, schemas, tests, visual evidence, dotnet tool packaging,
and self-contained release workflow are in place. Work should now be either a
scoped defect/maintenance change or an explicitly selected Phase 2 item.

## Prohibited shortcuts

- no one-off Python/Node DOCX generator in production;
- no Office Interop or COM in production (Word automation is permitted only
  for an explicit local visual-acceptance task);
- no unbounded Markdown/HTML renderer beyond ADR-0005's Phase 1 subset;
- no `--lenient` to make missing content disappear;
- no remote images/raw HTML without an explicit product requirement;
- no customer proposals in fixtures;
- no package without approved license review;
- no push or history rewrite without explicit user authorization.

## Agent-facing CLI contract

```text
inspect
  -> generate-schema (new template) or generate-schema --check (governed template)
  -> scaffold-model
  -> edit JSON/Markdown
  -> validate-model
  -> render --dry-run
  -> render --validate
```

Agents consume stdout JSON. Each failure is repaired from:

- `errorCode`;
- `diagnostics[].path`;
- `diagnostics[].message`;
- `diagnostics[].hint`.

Do not scrape human stderr or infer placeholder names. Do not infer business
formats or optionality from labels that are absent from the generated schema.

For reverse conversion, agents call `extract --file ... --out ... --json`,
review warning diagnostics, and edit the resulting Markdown plus its adjacent
assets directory. They must retain the source DOCX when exact Word layout,
headers/footers, comments, or tracked changes matter.

## Hooks

Hook entrypoints live in `eng/hooks`. They intentionally call the product's
`validate-model` command instead of reimplementing schema rules in shell.

Project lifecycle hooks may now call the implemented `validate-model` command.
Keep wrappers thin so the CLI remains the single validation implementation.

Suggested lifecycle:

- after editing `model.json` or `*.md`, run `validate-model`;
- before a commit, run `eng/verify`;
- before a release, run deterministic, license, package, and visual gates.

## Handoff format

At the end of work, state:

```text
Outcome:
Public contract changes:
Files:
Verification:
Unverified assumptions:
Next task:
Branch/commit:
```

Do not say “tests pass” unless the exact command was executed successfully.
