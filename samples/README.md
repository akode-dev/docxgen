# Samples

This folder contains synthetic authoring inputs. A reference DOCX template will
be added only after the P0 spike and branding review.

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
