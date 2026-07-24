namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Options passed to deterministic post-processors.</summary>
public sealed record PostProcessOptions
{
    /// <summary>Gets whether Word should update fields when the document opens.</summary>
    public bool UpdateFieldsOnOpen { get; init; } = true;

    /// <summary>Gets custom document properties to write.</summary>
    public IReadOnlyDictionary<string, string> DocumentProperties { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
