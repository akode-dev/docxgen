namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Stable diagnostic codes used in JSON output and compatibility tests.</summary>
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

    /// <summary>A referenced local asset does not exist.</summary>
    public const string AssetNotFound = "E-SEC-003";

    /// <summary>A remote image was blocked by default security policy.</summary>
    public const string RemoteImageBlocked = "W-SEC-003";

    /// <summary>An explicitly allowed remote image could not be downloaded safely.</summary>
    public const string RemoteImageDownloadFailed = "E-SEC-004";

    /// <summary>An optional template placeholder was not bound in lenient mode.</summary>
    public const string OptionalPlaceholderUnbound = "W-MDL-007";

    /// <summary>Raw HTML was removed from Markdown.</summary>
    public const string RawHtmlStripped = "W-MD-001";

    /// <summary>An unsupported Markdown feature was downgraded.</summary>
    public const string MarkdownFeatureDowngraded = "W-MD-002";

    /// <summary>A heading exceeded Word's supported heading levels.</summary>
    public const string HeadingLevelClamped = "W-MD-003";

    /// <summary>A section anchor has invalid syntax or attributes.</summary>
    public const string MarkdownInvalidSectionAnchor = "E-MD-004";

    /// <summary>Markdown content before the first section anchor was ignored.</summary>
    public const string MarkdownAnchorPreambleIgnored = "W-MD-005";

    /// <summary>An anchored table cannot be converted to a model collection.</summary>
    public const string MarkdownInvalidTableAnchor = "E-MD-006";

    /// <summary>A placeholder may be split across Word runs.</summary>
    public const string PlaceholderSplitAcrossRuns = "W-TPL-101";

    /// <summary>An unresolved placeholder remains in the output.</summary>
    public const string LeftoverPlaceholderInOutput = "W-OUT-001";

    /// <summary>Word must update TOC or other fields when the file opens.</summary>
    public const string TocRequiresWordUpdate = "W-OUT-002";

    /// <summary>An explicit model value overrides anchored Markdown.</summary>
    public const string ModelOverridesMarkdown = "W-MRG-001";

    /// <summary>The requested template identity does not match the inspected template.</summary>
    public const string TemplateIdentityMismatch = "E-TPL-004";

    /// <summary>A generated document failed Open XML validation.</summary>
    public const string OutputInvalidOoxml = "E-OUT-003";

    /// <summary>An input or output file could not be read or written.</summary>
    public const string IoFailure = "E-IO-001";

    /// <summary>Command-line arguments are invalid or contradictory.</summary>
    public const string InvalidUsage = "E-USG-001";

    /// <summary>The renderer failed unexpectedly for otherwise valid inputs.</summary>
    public const string RenderFailure = "E-RND-001";

    /// <summary>An input DOCX cannot be opened for semantic extraction.</summary>
    public const string ExtractionFailure = "E-EXT-001";

    /// <summary>A Word construct was skipped or represented with reduced semantics.</summary>
    public const string ExtractionFeatureDowngraded = "W-EXT-002";

    /// <summary>A generated Word field was omitted from Markdown.</summary>
    public const string ExtractionFieldOmitted = "W-EXT-003";

    /// <summary>A table header was inferred for valid GFM output.</summary>
    public const string ExtractionTableHeaderInferred = "W-EXT-004";

    /// <summary>A template has no statically discoverable model bindings.</summary>
    public const string SchemaNoBindings = "E-SCH-001";

    /// <summary>A checked generated schema is missing or out of date.</summary>
    public const string SchemaOutOfDate = "E-SCH-002";

    /// <summary>An unexpected internal product failure occurred.</summary>
    public const string UnexpectedFailure = "E-INT-001";
}
