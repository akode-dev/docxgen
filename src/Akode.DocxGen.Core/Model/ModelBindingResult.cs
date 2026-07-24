using System.Collections.ObjectModel;
using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Outcome of directive resolution and renderer-model materialization.</summary>
public sealed record ModelBindingResult
{
    /// <summary>Initializes a binding result.</summary>
    public ModelBindingResult(BoundModel? model, IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Model = model;
        Diagnostics = new ReadOnlyCollection<Diagnostic>(diagnostics.ToArray());
    }

    /// <summary>Gets the materialized model, or null after a blocking error.</summary>
    public BoundModel? Model { get; }

    /// <summary>Gets ordered binding diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets whether binding succeeded.</summary>
    public bool IsValid =>
        Model is not null
        && Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
