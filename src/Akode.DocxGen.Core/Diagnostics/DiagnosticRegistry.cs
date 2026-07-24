using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Authoritative metadata and factory for public diagnostics.</summary>
public static class DiagnosticRegistry
{
    private static readonly DiagnosticDescriptor[] descriptors =
    [
        new(
            DiagnosticCode.TemplateNotOoxml,
            DiagnosticSeverity.Error,
            "The template is not a valid DOCX OOXML package.",
            "Use a .docx template that opens without repair in Microsoft Word."),
        new(
            DiagnosticCode.TemplateMacroEnabled,
            DiagnosticSeverity.Error,
            "Macro-enabled templates are not allowed.",
            "Save the template as a macro-free .docx file and retry."),
        new(
            DiagnosticCode.TemplateSyntaxError,
            DiagnosticSeverity.Error,
            "Template placeholder syntax is invalid.",
            "Retype the reported placeholder as plain text and run inspect again."),
        new(
            DiagnosticCode.ModelInvalidJson,
            DiagnosticSeverity.Error,
            "The model JSON could not be parsed.",
            "Correct the JSON syntax at the reported location and validate it again."),
        new(
            DiagnosticCode.ModelSchemaViolation,
            DiagnosticSeverity.Error,
            "The model does not satisfy its JSON Schema.",
            "Update the reported value to satisfy the adjacent template schema."),
        new(
            DiagnosticCode.ModelUnboundPlaceholders,
            DiagnosticSeverity.Error,
            "Required template placeholders are not bound.",
            "Add values for the reported model paths or mark them optional in the template schema."),
        new(
            DiagnosticCode.ModelKindMismatch,
            DiagnosticSeverity.Error,
            "A model value has the wrong kind for its template placeholder.",
            "Change the reported value to the scalar, object, collection, Markdown, or asset kind expected by the template."),
        new(
            DiagnosticCode.ModelDuplicateSection,
            DiagnosticSeverity.Error,
            "An anchored Markdown section is declared more than once.",
            "Keep one section for the reported anchor or rename the duplicate anchor."),
        new(
            DiagnosticCode.ModelUnknownDirective,
            DiagnosticSeverity.Error,
            "The model contains an unknown reserved directive.",
            "Replace the reported key with an approved $md, $mdFile, $file, or $text directive."),
        new(
            DiagnosticCode.AssetOutsideRoot,
            DiagnosticSeverity.Error,
            "An asset path resolves outside the approved assets root.",
            "Move the asset under the configured assets directory and reference it with a contained relative path."),
        new(
            DiagnosticCode.AssetTooLarge,
            DiagnosticSeverity.Error,
            "An asset exceeds the configured size limit.",
            "Reduce the asset size or raise the explicit limit after reviewing resource and security impact."),
        new(
            DiagnosticCode.RemoteImageBlocked,
            DiagnosticSeverity.Warning,
            "A remote image was blocked by the default offline policy.",
            "Download the image into the approved assets directory or explicitly enable remote images."),
        new(
            DiagnosticCode.RawHtmlStripped,
            DiagnosticSeverity.Warning,
            "Raw HTML was removed from Markdown.",
            "Express the content with supported Markdown or explicitly enable reviewed raw HTML input."),
        new(
            DiagnosticCode.MarkdownFeatureDowngraded,
            DiagnosticSeverity.Warning,
            "An unsupported Markdown feature was rendered with reduced semantics.",
            "Replace the reported construct with the supported Phase 1 Markdown subset."),
        new(
            DiagnosticCode.HeadingLevelClamped,
            DiagnosticSeverity.Warning,
            "A heading exceeded Word's supported heading levels.",
            "Reduce the source heading depth or choose a smaller heading offset."),
        new(
            DiagnosticCode.PlaceholderSplitAcrossRuns,
            DiagnosticSeverity.Warning,
            "A template placeholder may be split across Word runs.",
            "Retype the complete placeholder in one operation as plain text."),
        new(
            DiagnosticCode.LeftoverPlaceholderInOutput,
            DiagnosticSeverity.Warning,
            "An unresolved placeholder remains in the generated document.",
            "Bind the reported path or remove the unused placeholder from the template."),
        new(
            DiagnosticCode.TocRequiresWordUpdate,
            DiagnosticSeverity.Warning,
            "Word must update the table of contents or other fields.",
            "Open the document in Microsoft Word and update all fields before final delivery."),
        new(
            DiagnosticCode.ModelOverridesMarkdown,
            DiagnosticSeverity.Warning,
            "An explicit model value overrides anchored Markdown.",
            "Remove one source or confirm that model-over-Markdown precedence is intended."),
    ];

    private static readonly IReadOnlyList<DiagnosticDescriptor> readOnlyDescriptors =
        Array.AsReadOnly(descriptors);

    private static readonly FrozenDictionary<string, DiagnosticDescriptor> descriptorsByCode =
        descriptors.ToFrozenDictionary(descriptor => descriptor.Code, StringComparer.Ordinal);

    /// <summary>Gets all registered descriptors in stable code order.</summary>
    public static IReadOnlyList<DiagnosticDescriptor> All => readOnlyDescriptors;

    /// <summary>Gets metadata for a registered code.</summary>
    /// <exception cref="KeyNotFoundException">The code is not registered.</exception>
    public static DiagnosticDescriptor Get(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return descriptorsByCode.TryGetValue(code, out var descriptor)
            ? descriptor
            : throw new KeyNotFoundException($"Diagnostic code '{code}' is not registered.");
    }

    /// <summary>Attempts to get metadata for a registered code.</summary>
    public static bool TryGet(
        string? code,
        [NotNullWhen(true)] out DiagnosticDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            descriptor = null;
            return false;
        }

        return descriptorsByCode.TryGetValue(code, out descriptor);
    }

    /// <summary>Creates a diagnostic from registered defaults and optional context.</summary>
    public static Diagnostic Create(
        string code,
        string? path = null,
        string? message = null,
        string? hint = null)
    {
        var descriptor = Get(code);
        var resolvedMessage = message ?? descriptor.DefaultMessage;
        var resolvedHint = hint ?? descriptor.DefaultHint;

        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedHint);

        return new Diagnostic(
            descriptor.Code,
            descriptor.Severity,
            resolvedMessage,
            resolvedHint,
            path);
    }
}
