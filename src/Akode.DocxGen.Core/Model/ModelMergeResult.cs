using System.Collections.ObjectModel;
using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Immutable data tree produced by source-precedence reconciliation.</summary>
public sealed record ModelMergeResult
{
    /// <summary>Initializes a model merge result.</summary>
    public ModelMergeResult(
        IReadOnlyDictionary<string, ModelValue> data,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Data = new ReadOnlyDictionary<string, ModelValue>(
            new Dictionary<string, ModelValue>(data, StringComparer.Ordinal));
        Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics.ToArray());
    }

    /// <summary>Gets merged root values.</summary>
    public IReadOnlyDictionary<string, ModelValue> Data { get; }

    /// <summary>Gets parsing, conversion, and precedence diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets whether no blocking merge diagnostic was emitted.</summary>
    public bool IsValid =>
        Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
