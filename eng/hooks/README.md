# Agent hook entrypoints

The wrappers in this folder are intentionally not activated yet. They call the
future `validate-model` CLI command so that hook behavior and normal CLI
behavior cannot drift.

Activate project hooks only after `validate-model` has process-level tests for
JSON output and exit codes.

Example future call:

```powershell
./eng/hooks/validate-model.ps1 `
  -Template templates/proposal.docx `
  -Model samples/model.json `
  -AssetsDir samples
```
