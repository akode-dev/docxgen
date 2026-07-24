# Agent integration

## Durable instructions

- Codex automatically reads `AGENTS.md` and may read closer nested files for a
  subtree.
- Claude Code uses `CLAUDE.md`; the root file imports shared repository rules.
- Product details belong in `docs`, not in oversized root agent instructions.

References:

- [Codex AGENTS.md guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Claude Code setup](https://docs.anthropic.com/en/docs/claude-code/getting-started)

## Local agent setup

```powershell
git checkout develop
dotnet restore Akode.DocxGen.sln
dotnet build Akode.DocxGen.sln --configuration Release --no-restore
```

Start Codex:

```powershell
codex
```

Start Claude Code:

```powershell
claude
```

Both agents should receive a bounded task tied to an implementation-plan
acceptance slice.

## Hook design

The lifecycle hook calls:

```text
docxgen validate-model --template ... --model ... --assets-dir ... --json
```

It must use the CLI exit code and JSON result. It must not implement a second
JSON validator in shell.

The JSON result follows the [machine-readable report format](report-format.md).
Hooks read the numeric `exitCode`, top-level `errorCode`/`hint`, and ordered
`diagnostics`; they do not scrape stderr.

Tracked wrappers under `eng/hooks` make this portable. Project-specific hook
configuration is added only after the command works.

### Codex

Codex project hooks may be configured under `.codex`, but project-local config
loads only for a trusted repository. Keep hook commands relative to the Git
root and cross-platform where practical.

### Claude Code

Shared command permissions live in `.claude/settings.json`. Local approvals and
secrets belong in ignored `.claude/settings.local.json`. Destructive Git
operations and push remain denied by project defaults.

## Non-interactive execution

Agents may run in CI or hosted jobs. The task prompt should name:

- issue/acceptance slice;
- allowed scope;
- required verification;
- whether commit/branch/PR actions are authorized;
- artifacts to retain.

Non-interactive agents must never receive credentials through committed files.

## Acceptance scenario

A fresh agent session should be able to:

1. discover the repository rules;
2. identify the current implementation phase;
3. build and test the solution;
4. implement one scoped task in the correct layer;
5. update tests and relevant docs;
6. provide an evidence-based handoff.

For the finished product, a proposal-generation task must complete using the
DocxGen commands without generating an ad-hoc document script.
