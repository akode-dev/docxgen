# Claude Code instructions

@AGENTS.md
@docs/agent-guide.md

Use the repository rules above as the source of truth. Start by checking the
current branch and working tree, then select the smallest acceptance slice from
`docs/implementation-plan.md`.

Do not generate ad-hoc Python, Node.js, or shell-based DOCX converters. The
product goal is a deterministic .NET tool. Do not bypass failures with
`--lenient`, `--allow-remote-images`, or `--allow-raw-html`.

Use the implemented machine-readable workflow:

```text
inspect -> generate-schema/check -> scaffold-model -> validate-model
        -> render --dry-run -> render
```

Read JSON failures, fix the path named in `errors[].path`, follow
`errors[].hint`, and retry. Never parse human-oriented stderr when `--json` is
available.
