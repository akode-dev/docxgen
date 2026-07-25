# Security policy

## Supported versions

Security fixes are applied to the latest published release and the current
development line. Older versions may be asked to upgrade before a fix is
provided.

## Reporting a vulnerability

Do not open a public issue. Use GitHub's private vulnerability reporting for
`akode-dev/docxgen` when available, or contact the maintainers privately
through the repository profile.

Include:

- the affected version and operating system;
- a minimal reproduction or proof of concept;
- the expected and observed behavior;
- the likely impact;
- any suggested mitigation;
- whether the report may be credited publicly.

Do not include real customer documents, credentials, access tokens, or
personal data. Use a synthetic DOCX and local assets.

The maintainers will acknowledge a complete report as soon as practical,
investigate it, coordinate a fix and disclosure, and credit the reporter when
requested. Please allow a reasonable remediation period before publishing
details.

## Security model

DocxGen treats DOCX, JSON, Markdown, and image inputs as untrusted. Rendering
is offline by default, local assets are path-contained and resource-limited,
remote images require an explicit opt-in, raw HTML is not interpreted as
OOXML, and generated files can be validated with Open XML SDK.

These controls reduce risk but do not make arbitrary input safe for every
environment. Run the CLI with least privilege and isolate it when processing
untrusted documents at scale.
