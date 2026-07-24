# Agent hook entrypoints

The wrappers call the implemented `validate-model` CLI command so hook
behavior and normal CLI behavior cannot drift. They can be attached to an
agent lifecycle or pre-commit workflow as needed.

Example call:

```powershell
./eng/hooks/validate-model.ps1 `
  -Template templates/proposal.docx `
  -Model samples/model.json `
  -AssetsDir samples
```
