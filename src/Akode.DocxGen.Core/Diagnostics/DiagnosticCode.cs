namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Registry of stable diagnostics used in JSON output and tests.</summary>
public static class DiagnosticCode
{
    /// <summary>Template is not a valid OOXML package.</summary>
    public const string TemplateNotOoxml = "E-TPL-001";

    /// <summary>Macro-enabled templates are forbidden.</summary>
    public const string TemplateMacroEnabled = "E-TPL-002";

    /// <summary>Template placeholder syntax is invalid.</summary>
    public const string TemplateSyntaxError = "E-TPL-003";

    /// <summary>Model JSON cannot be parsed.</summary>
    public const string ModelInvalidJson = "E-MDL-001";

    /// <summary>Model does not satisfy its JSON Schema.</summary>
    public const string ModelSchemaViolation = "E-MDL-002";

    /// <summary>Required template placeholders are not bound.</summary>
    public const string ModelUnboundPlaceholders = "E-MDL-003";

    /// <summary>Model value kind does not match the template contract.</summary>
    public const string ModelKindMismatch = "E-MDL-004";

    /// <summary>An anchored Markdown section is duplicated.</summary>
    public const string ModelDuplicateSection = "E-MDL-005";

    /// <summary>A reserved model directive is unknown.</summary>
    public const string ModelUnknownDirective = "E-MDL-006";

    /// <summary>An asset path escapes the approved root.</summary>
    public const string AssetOutsideRoot = "E-SEC-001";

    /// <summary>An asset exceeds configured limits.</summary>
    public const string AssetTooLarge = "E-SEC-002";

    /// <summary>A remote image was blocked by default security policy.</summary>
    public const string RemoteImageBlocked = "W-SEC-003";

    /// <summary>Raw HTML was removed from Markdown.</summary>
    public const string RawHtmlStripped = "W-MD-001";

    /// <summary>An unsupported Markdown feature was downgraded.</summary>
    public const string MarkdownFeatureDowngraded = "W-MD-002";

    /// <summary>A heading exceeded Word's supported heading levels.</summary>
    public const string HeadingLevelClamped = "W-MD-003";

    /// <summary>A placeholder may be split across Word runs.</summary>
    public const string PlaceholderSplitAcrossRuns = "W-TPL-101";

    /// <summary>An unresolved placeholder remains in the output.</summary>
    public const string LeftoverPlaceholderInOutput = "W-OUT-001";

    /// <summary>Word must update TOC or other fields when the file opens.</summary>
    public const string TocRequiresWordUpdate = "W-OUT-002";

    /// <summary>An explicit model value overrides anchored Markdown.</summary>
    public const string ModelOverridesMarkdown = "W-MRG-001";
}
