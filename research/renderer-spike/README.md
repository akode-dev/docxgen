# Renderer spike

This harness proves or rejects ADR-0001 with a representative, non-confidential
template. It is evidence code, not production architecture.

## Build the synthetic template

Use the repository's configured Python runtime with `python-docx`:

```powershell
python .\build_template.py
```

## Run

From the repository root:

```powershell
dotnet run --project .\spike\Akode.DocxGen.RendererSpike -- `
  .\spike\Akode.DocxGen.RendererSpike\assets\proposal-template.docx `
  .\spike\Akode.DocxGen.RendererSpike\assets\model.json `
  .\spike\Akode.DocxGen.RendererSpike\assets\body.md `
  .\spike\Akode.DocxGen.RendererSpike\assets\architecture.svg `
  .\spike\Akode.DocxGen.RendererSpike\artifacts\proposal-rendered.docx
```

`DocxTemplater.Images` is deliberately not referenced because its transitive
ImageSharp license is outside the repository allow-list. The spike inserts the
single local SVG figure with a bounded Open XML post-processor.
