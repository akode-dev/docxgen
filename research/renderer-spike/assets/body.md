# Executive summary

Northwind Group needs a **durable knowledge platform** that makes governed content easy to find, maintain, and reuse. The proposed solution combines a structured authoring workflow with automated document generation.

The outcome is a consistent publication pipeline with:

- clear ownership and approval;
- reusable source material;
  - Markdown for narrative content;
  - JSON for structured metadata;
- repeatable, template-controlled Word output.

> Design belongs in the Word template; content belongs in Markdown and JSON.

## Objectives and scope

The first release covers three objectives:

1. Validate every input before rendering.
2. Produce a professional DOCX from an approved template.
   1. Preserve cover, headers, footers, and final page.
   2. Update document fields when Word opens the file.
3. Provide deterministic diagnostics for people and agents.

### Acceptance matrix

| Capability | Phase 1 | Evidence |
|---|:---:|---|
| Metadata binding | Yes | Cover and Document Control |
| Markdown headings | Yes | Heading 1 through Heading 4 |
| Nested lists | Yes | Numbering and indentation |
| Local figure | Yes | Inline, aspect-preserving image |
| Remote image | No | Blocked by default |

#### Technical note

The renderer uses the DocxTemplater Markdown formatter for Markdown-to-OOXML conversion. The exact package identifier appears as inline code below.

`DocxTemplater.Markdown`

Variable figures are added by a bounded Open XML adapter so the production dependency graph remains license-compliant.

```csharp
var request = new RenderRequest(template, markdown, model);
var result = await renderer.RenderAsync(request, cancellationToken);
```

For the public command contract, see the [CLI reference](https://example.test/docxgen/cli). Raw HTML must remain inert or be rejected; the fixture includes an inline script example below.

<script>alert('disabled')</script>

## Rendering pipeline

The following figure is inserted after Markdown rendering from a local file:

[[DOCXGEN_IMAGE:architecture.svg]]

*Figure 1 — Deterministic document-generation pipeline.*

## Delivery approach

Work is divided into small, independently verifiable slices. Each slice has an input fixture, an expected semantic result, structural DOCX validation, and a rendered-page review.

### Operational considerations

- The CLI runs offline by default.
- Output is written atomically.
- Version suffixes are optional and filesystem-safe.
- Validation reports are suitable for both humans and AI agents.

#### Closing observation

Long documents are represented by ordinary Markdown headings and paragraphs. There is no need to enumerate chapters in JSON unless a template explicitly needs structured chapter metadata.
