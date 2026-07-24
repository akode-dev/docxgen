using System.Collections.ObjectModel;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for the complete render preflight and document pipeline.</summary>
public sealed record RenderRequest
{
    /// <summary>Initializes a render request.</summary>
    public RenderRequest(
        InputArtifact template,
        InputArtifact? model,
        InputArtifact? markdown,
        string assetsRoot,
        RenderOptions options,
        IReadOnlyDictionary<string, string>? overrides = null,
        IReadOnlyDictionary<string, string>? documentProperties = null,
        bool validateOutput = false,
        bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (model is null && markdown is null)
        {
            throw new ArgumentException(
                "At least one model or Markdown input is required.",
                nameof(model));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(assetsRoot);
        ArgumentNullException.ThrowIfNull(options);

        Template = template;
        Model = model;
        Markdown = markdown;
        AssetsRoot = assetsRoot;
        Options = options;
        Overrides = Copy(overrides);
        DocumentProperties = Copy(documentProperties);
        ValidateOutput = validateOutput;
        DryRun = dryRun;
    }

    /// <summary>Gets the DOCX template input.</summary>
    public InputArtifact Template { get; }

    /// <summary>Gets optional JSON model input.</summary>
    public InputArtifact? Model { get; }

    /// <summary>Gets optional standalone or anchored Markdown input.</summary>
    public InputArtifact? Markdown { get; }

    /// <summary>Gets the approved root for local input assets.</summary>
    public string AssetsRoot { get; }

    /// <summary>Gets preprocessing and rendering options.</summary>
    public RenderOptions Options { get; }

    /// <summary>Gets highest-precedence model overrides.</summary>
    public IReadOnlyDictionary<string, string> Overrides { get; }

    /// <summary>Gets custom document properties for post-processing.</summary>
    public IReadOnlyDictionary<string, string> DocumentProperties { get; }

    /// <summary>Gets whether output OOXML validation is requested.</summary>
    public bool ValidateOutput { get; }

    /// <summary>Gets whether preflight should stop before returning document bytes.</summary>
    public bool DryRun { get; }

    private static ReadOnlyDictionary<string, string> Copy(
        IReadOnlyDictionary<string, string>? source)
    {
        var copy = source is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(source, StringComparer.Ordinal);
        return new ReadOnlyDictionary<string, string>(copy);
    }
}
