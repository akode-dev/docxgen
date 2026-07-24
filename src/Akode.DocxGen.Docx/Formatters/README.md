# Formatters

This folder will contain registration and small adapter-level formatters for
DocxTemplater scalar/template behavior.

The `MD` slot remains a public template marker, but the rejected
`DocxTemplater.Markdown` and `DocxTemplater.Images` packages must not be
registered. Markdown normalization and security policy belong in
`Akode.DocxGen.Core/Markdown`; bounded OOXML block rendering belongs in the
Docx project's planned `Markdown` folder per ADR-0005.
