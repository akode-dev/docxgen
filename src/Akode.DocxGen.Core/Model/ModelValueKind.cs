namespace Akode.DocxGen.Core.Model;

/// <summary>Semantic kind of a value after model parsing.</summary>
public enum ModelValueKind
{
    /// <summary>Plain text.</summary>
    Text,

    /// <summary>Markdown content.</summary>
    Markdown,

    /// <summary>Binary content.</summary>
    Binary,

    /// <summary>An ordered collection.</summary>
    Collection,

    /// <summary>A structured object.</summary>
    StructuredObject,
}
