namespace Akode.DocxGen.Core.Model;

/// <summary>A parsed DocxGen model envelope ready for asset resolution.</summary>
public sealed record ModelDocument(
    string ModelVersion,
    TemplateReference Template,
    ModelDocumentOptions Options,
    IReadOnlyDictionary<string, ModelValue> Data);

/// <summary>Identity of the template contract requested by a model.</summary>
public sealed record TemplateReference(string Id, string Version);

/// <summary>Model-supplied preprocessing defaults.</summary>
public sealed record ModelDocumentOptions(
    string Culture,
    bool Strict,
    int HeadingOffset,
    bool AllowRawHtml,
    bool AllowRemoteImages,
    bool UpdateFieldsOnOpen)
{
    /// <summary>Default safe model options.</summary>
    public static ModelDocumentOptions Default { get; } = new(
        "en-US",
        Strict: true,
        HeadingOffset: 0,
        AllowRawHtml: false,
        AllowRemoteImages: false,
        UpdateFieldsOnOpen: true);
}
