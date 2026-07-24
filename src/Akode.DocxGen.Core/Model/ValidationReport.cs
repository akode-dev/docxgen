using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Result of OOXML package validation.</summary>
public sealed record ValidationReport(
    bool IsValid,
    IReadOnlyList<Diagnostic> Diagnostics);
