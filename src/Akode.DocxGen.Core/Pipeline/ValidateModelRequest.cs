namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for model validation without document generation.</summary>
public sealed record ValidateModelRequest
{
    /// <summary>Initializes a model-validation request.</summary>
    public ValidateModelRequest(
        InputArtifact template,
        InputArtifact model,
        InputArtifact? markdown,
        string assetsRoot,
        RenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetsRoot);
        ArgumentNullException.ThrowIfNull(options);

        Template = template;
        Model = model;
        Markdown = markdown;
        AssetsRoot = assetsRoot;
        Options = options;
    }

    /// <summary>Gets the DOCX template input.</summary>
    public InputArtifact Template { get; }

    /// <summary>Gets the JSON model input.</summary>
    public InputArtifact Model { get; }

    /// <summary>Gets optional anchored Markdown.</summary>
    public InputArtifact? Markdown { get; }

    /// <summary>Gets the approved root for local input assets.</summary>
    public string AssetsRoot { get; }

    /// <summary>Gets preprocessing and reconciliation options.</summary>
    public RenderOptions Options { get; }
}
