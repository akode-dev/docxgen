using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Renderer output before the CLI writes it atomically.</summary>
public sealed record RenderOutcome(
    Stream Document,
    IReadOnlyList<string> BoundPaths,
    IReadOnlyList<string> UnboundPaths,
    MarkdownStats MarkdownStats,
    IReadOnlyList<Diagnostic> Diagnostics);
