# Templates

`proposal.docx` is the synthetic, non-confidential Phase 1 reference template.
It demonstrates the required package shape and is paired with
`proposal.schema.json`. It is not an approved corporate brand master; replace
it with the approved template only after the Word acceptance pass.

Each production template is paired with a schema:

```text
proposal.docx
proposal.schema.json
```

The reference proposal demonstrates this recommended shape:

1. branded cover;
2. Document Control or explicit variant without it;
3. Word TOC;
4. standalone `{{ds.Body}:MD}`;
5. next-page section break;
6. branded final page.

This is not a universal engine requirement. Other governed templates may have
a single placeholder, one or more Markdown slots, nested collections, or no
proposal-specific cover/Document Control/TOC pages. Generate their adjacent
contract with `docxgen generate-schema`.

Do not add real customer documents to this folder.
