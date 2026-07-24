using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Generated model JSON and optional named Markdown stub contents.</summary>
public sealed record ScaffoldModelResult(
    string TemplateHash,
    string ModelJson,
    IReadOnlyDictionary<string, string> MarkdownStubs,
    IReadOnlyList<Diagnostic> Diagnostics);
