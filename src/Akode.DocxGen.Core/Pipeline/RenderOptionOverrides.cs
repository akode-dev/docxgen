namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Identifies caller values that override model-supplied defaults.</summary>
[Flags]
public enum RenderOptionOverrides
{
    /// <summary>No caller value overrides the model.</summary>
    None = 0,

    /// <summary>The formatting culture is caller-supplied.</summary>
    Culture = 1 << 0,

    /// <summary>The strict binding mode is caller-supplied.</summary>
    Strict = 1 << 1,

    /// <summary>The Markdown heading offset is caller-supplied.</summary>
    HeadingOffset = 1 << 2,

    /// <summary>The raw HTML policy is caller-supplied.</summary>
    AllowRawHtml = 1 << 3,

    /// <summary>The remote image policy is caller-supplied.</summary>
    AllowRemoteImages = 1 << 4,

    /// <summary>The Word field-update policy is caller-supplied.</summary>
    UpdateFieldsOnOpen = 1 << 5,

    /// <summary>Every caller value overrides the model.</summary>
    All = Culture
        | Strict
        | HeadingOffset
        | AllowRawHtml
        | AllowRemoteImages
        | UpdateFieldsOnOpen,
}
