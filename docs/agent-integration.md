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
dotnet restore DocxGen.sln
dotnet build DocxGen.sln --configuration Release --no-restore
```

Start Codex:

```powershell
codex
```

Start Claude Code:

```powershell
claude
```

Both agents should receive a bounded maintenance task or an explicitly
selected roadmap item.

## Product tool identity

Use one stable identity across agent hosts:

| Setting | Value |
|---|---|
| Friendly tool name | `DocxGen` |
| Executable | `docxgen` (`docxgen.exe` on Windows) |
| Dotnet tool package | `Akode.DocxGen.Tool` |
| Ready-to-use library package | `Akode.DocxGen` |

`DocxGen` is the name shown to the model or placed in a host allow-list.
`docxgen` is the actual process command. A shell-capable agent can invoke the
CLI directly; a future MCP adapter will expose typed operations without
shelling out.

Install the CLI in an isolated agent workspace:

```shell
dotnet tool install Akode.DocxGen.Tool --tool-path .tools
```

Then add `.tools` to the process `PATH` or configure the agent host with the
absolute `.tools/docxgen` path. Pin the package version in production.

Grant only the operations needed by the task. A document-authoring agent
normally needs:

```text
docxgen inspect ...
docxgen generate-schema ...
docxgen scaffold-model ...
docxgen validate-model ...
docxgen render ...
```

Add `convert`, `extract`, or `validate` only for workflows that use them.
Repository-wide behavior lives in `AGENTS.md` and `CLAUDE.md`. Personal
`.claude/` and `.codex/` configuration is intentionally ignored and must be
created locally when needed.

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
configuration may invoke the implemented command.

### Codex

Codex project hooks may be configured locally under `.codex`, but
project-local config loads only for a trusted repository. `AGENTS.md` is the
durable repository instruction surface; a reusable DocxGen workflow belongs
in a skill, while a typed named tool belongs in the planned MCP adapter. Keep
hook commands relative to the Git root and cross-platform where practical.

### Claude Code

Personal command permissions, approvals, and secrets belong in the ignored
`.claude/` directory. Shared engineering constraints remain in `CLAUDE.md` and
`AGENTS.md`.

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

For the finished product, a document-generation task must complete using the
DocxGen commands without generating an ad-hoc document script.
