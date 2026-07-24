using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Template inspection result before CLI report projection.</summary>
public sealed record InspectResult(
    TemplateSchema Schema,
    IReadOnlyList<string> UnsupportedForStaticAnalysis);
