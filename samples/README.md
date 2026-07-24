# Samples

This folder contains synthetic authoring inputs for the committed reference
template. `architecture.svg` demonstrates bounded local-image insertion.

Planned execution:

```powershell
docxgen validate-model `
  --template ../templates/proposal.docx `
  --model model.json `
  --assets-dir . `
  --json

docxgen render `
  --template ../templates/proposal.docx `
  --model model.json `
  --assets-dir . `
  --out ../out/Proposal.docx `
  --append-document-version `
  --validate `
  --json
```
