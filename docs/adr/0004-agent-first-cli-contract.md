# ADR-0004: Agent-first CLI contract

- Status: Accepted
- Date: 2026-07-24

## Decision

The Phase 1 integration surface is a non-interactive CLI with:

- JSON stdout;
- human logs on stderr;
- stable exit codes;
- stable diagnostic codes;
- exact JSON paths;
- a concrete `hint` for every actionable error;
- `inspect`, `scaffold-model`, `validate-model`, `render`, and `validate`;
- no network by default.

MCP is a Phase 2 adapter over the same Core.

## Rationale

The CLI works locally, in CI, and in different coding-agent products. Clear
machine contracts let an agent repair its input without generating fallback
scripts or asking a human to interpret unstructured errors.

## Consequences

CLI output and diagnostics are public API. Changes require compatibility tests
and documentation updates.
