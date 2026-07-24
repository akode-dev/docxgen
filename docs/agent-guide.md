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

## First implementation task

The first coding task is P0, not production CLI structure:

```text
Prove Markdown -> branded DOCX behavior with DocxTemplater 2.8.3.
```

The evidence must cover styles, lists, tables, images, heading levels, TOC,
headers, footers, text boxes, cover, final page, Open XML validation, and
visual rendering.

## Prohibited shortcuts

- no one-off Python/Node DOCX generator;
- no Office Interop or COM;
- no custom Markdown-to-OOXML engine before ADR-0001 is superseded;
- no `--lenient` to make missing content disappear;
- no remote images/raw HTML without an explicit product requirement;
- no customer proposals in fixtures;
- no package without approved license review;
- no push or history rewrite without explicit user authorization.

## Agent-facing CLI contract

Once implemented:

```text
inspect
  -> scaffold-model
  -> edit JSON/Markdown
  -> validate-model
  -> render --dry-run
  -> render --validate
```

Agents consume stdout JSON. Each failure is repaired from:

- `errorCode`;
- `errors[].path`;
- `errors[].message`;
- `errors[].hint`.

Do not scrape human stderr or infer placeholder names.

## Hooks

Hook entrypoints live in `eng/hooks`. They intentionally call the product's
`validate-model` command instead of reimplementing schema rules in shell.

Do not activate a project lifecycle hook until `validate-model` is implemented
and its exit-code/JSON tests pass; an active nonfunctional hook would block all
agent work.

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
