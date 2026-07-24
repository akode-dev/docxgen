using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Contract discovered from a DOCX template.</summary>
public sealed record TemplateSchema(
    string TemplateHash,
    IReadOnlyList<TemplatePlaceholder> Placeholders,
    IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>A placeholder expected by a template.</summary>
public sealed record TemplatePlaceholder(
    string Path,
    ModelValueKind Kind,
    string? Formatter,
    bool Required,
    IReadOnlyList<string> Locations);
