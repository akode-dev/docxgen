# Codex CLI, app, and cloud execution

## Repository contract

The repository does not pin a personal model, account, API key, or provider.
Those are user/workspace concerns. It commits only durable project guidance and
safe build commands.

Codex project guidance:

- root `AGENTS.md`;
- optional closer `AGENTS.md` files when subtree rules differ;
- `.codex/config.toml` for minimal trusted-project settings;
- `docs/agent-guide.md` for the working lifecycle.

Official Codex guidance treats `AGENTS.md` as the durable repository convention
surface. Project `.codex` configuration and hooks are loaded only for trusted
repositories.

## Local Codex CLI

```powershell
git checkout develop
./eng/verify.ps1
codex
```

The agent should begin with `git status`, read the implementation plan, and
work on a scoped issue.

## Codex desktop app

Open the repository root as the workspace. The same `AGENTS.md` applies. Use
the app for planning, review, visual fixture inspection, and long-running local
work, while retaining the same branch and verification policy.

## Hosted/cloud task

Configure:

- repository access;
- .NET 10 SDK;
- NuGet access to nuget.org and any explicitly approved private feed;
- no production/customer secrets for normal builds;
- optional artifact retention for test logs and synthetic rendered fixtures.

Recommended setup command:

```bash
dotnet restore DocxGen.sln
```

Recommended verification command:

```bash
./eng/verify.sh
```

Cloud tasks should work on a feature branch from `develop` and return a diff or
pull request. They must not push unless the task explicitly grants that action.

## Secrets

Never commit:

- OpenAI or Anthropic credentials;
- private package-feed access tokens;
- customer documents;
- confidential generated output;
- `.claude/settings.local.json`;
- local `.env` variants.

Use the host's encrypted secret/environment facility. Build and unit tests
should not require LLM credentials because DocxGen does not call an LLM.

## Network policy

Development restore needs approved package feeds. Product rendering is offline
by default. Tests must be structured so a renderer cannot make network calls.

## Environment parity

Local and hosted environments run the same:

```text
restore -> build Release -> test Release
```

Word-dependent acceptance is a separate manual Windows gate. Optional
LibreOffice visual rendering is a test/QA dependency, not a production runtime
dependency.
