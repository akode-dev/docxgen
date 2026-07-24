using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Reports;

/// <summary>Success data returned by <c>inspect</c>.</summary>
public sealed record InspectReportData(
    string Template,
    string? TemplateId,
    string? TemplateVersion,
    string TemplateHash,
    IReadOnlyList<TemplatePlaceholder> Placeholders,
    IReadOnlyList<string> RequiredStyles,
    IReadOnlyList<string> UnsupportedForStaticAnalysis,
    long DurationMs);

/// <summary>Success data returned by <c>scaffold-model</c>.</summary>
public sealed record ScaffoldModelReportData(
    string Output,
    string TemplateHash,
    IReadOnlyList<string> MarkdownStubs,
    long DurationMs);

/// <summary>Success data returned by <c>validate-model</c>.</summary>
public sealed record ValidateModelReportData(
    string TemplateHash,
    string ModelHash,
    IReadOnlyList<string> Bound,
    IReadOnlyList<string> Unbound,
    MarkdownStats MarkdownStats,
    long DurationMs);

/// <summary>Compact package-validation counts embedded in command reports.</summary>
public sealed record DocumentValidationSummary(
    bool IsValid,
    int ErrorCount,
    int WarningCount);

/// <summary>Success data returned by <c>render</c>.</summary>
public sealed record RenderReportData(
    string? Output,
    long OutputBytes,
    string TemplateHash,
    string? ModelHash,
    long DurationMs,
    IReadOnlyList<string> Bound,
    IReadOnlyList<string> Unbound,
    MarkdownStats MarkdownStats,
    DocumentValidationSummary? Validation,
    bool DryRun,
    string? DocumentVersion);

/// <summary>Success data returned by <c>convert</c>.</summary>
public sealed record ConvertReportData(
    string Output,
    long OutputBytes,
    long DurationMs,
    MarkdownStats MarkdownStats,
    DocumentValidationSummary? Validation);

/// <summary>Success data returned by <c>validate</c>.</summary>
public sealed record ValidateDocumentReportData(
    string File,
    long DurationMs,
    DocumentValidationSummary Validation);
