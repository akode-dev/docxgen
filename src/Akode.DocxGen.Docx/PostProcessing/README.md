# Post-processing

Planned deterministic post-processors:

- set `w:updateFields`;
- write core and custom document properties;
- normalize generated table and image geometry;
- remove marker-only empty paragraphs;
- scan the finished package for unresolved `{{...}}` placeholders.

Every processor implements `IDocumentPostProcessor` and has a stable `Order`.
