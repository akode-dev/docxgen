# Templates

`proposal.docx` is a synthetic, non-confidential reference template. It
demonstrates a complex package shape and is paired with
`proposal.schema.json`. It is not an approved corporate brand master.

Each production template is paired with a schema:

```text
template.docx
template.schema.json
```

Generated schemas describe every detected property as optional. Promote only
genuine business requirements to JSON Schema `required` arrays. The reference
proposal keeps its cover metadata and revision history optional so a Markdown
body can be rendered without a separate JSON model; when those objects are
supplied, their formats and collection-item rules are still validated.

The reference proposal demonstrates one possible shape:

1. branded cover;
2. Document Control or explicit variant without it;
3. Word TOC;
4. standalone `{{ds.Body}:MD}`;
5. next-page section break;
6. branded final page.

This is not a universal engine requirement. Other governed templates may have
a single placeholder, one or more Markdown slots, nested collections, or any
page sequence. Generate their adjacent contract with
`docxgen generate-schema`.

Do not add confidential or customer documents to this folder.
