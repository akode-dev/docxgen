using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Contract discovered from a DOCX template.</summary>
public sealed record TemplateSchema(
    string? TemplateId,
    string? TemplateVersion,
    string TemplateHash,
    IReadOnlyList<TemplatePlaceholder> Placeholders,
    IReadOnlyList<string> RequiredStyles,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the hierarchical data shape discovered through static template
    /// analysis.
    /// </summary>
    public IReadOnlyList<TemplateShapeNode> Roots { get; init; } = [];
}

/// <summary>A placeholder expected by a template.</summary>
public sealed record TemplatePlaceholder(
    string Path,
    ModelValueKind Kind,
    string? Formatter,
    string? FormatterArguments,
    bool Required,
    IReadOnlyList<string> Locations,
    IReadOnlyList<string> ItemProperties,
    string? UsedIn);

/// <summary>A hierarchical model binding discovered in a DOCX template.</summary>
public sealed record TemplateShapeNode
{
    /// <summary>Initializes an immutable template-shape node.</summary>
    public TemplateShapeNode(
        string name,
        ModelValueKind kind,
        IEnumerable<TemplateShapeNode>? properties = null,
        TemplateShapeNode? item = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Kind = kind;
        Properties = Array.AsReadOnly(
            (properties ?? [])
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray());
        Item = item;
    }

    /// <summary>Gets the model member name.</summary>
    public string Name { get; }

    /// <summary>Gets the semantic member kind.</summary>
    public ModelValueKind Kind { get; }

    /// <summary>Gets object properties in stable ordinal order.</summary>
    public IReadOnlyList<TemplateShapeNode> Properties { get; }

    /// <summary>Gets the collection item shape, when applicable.</summary>
    public TemplateShapeNode? Item { get; }
}
