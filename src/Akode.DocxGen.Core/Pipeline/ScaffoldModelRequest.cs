namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for generating an editable model skeleton from a template.</summary>
public sealed record ScaffoldModelRequest
{
    /// <summary>Initializes a model-scaffolding request.</summary>
    public ScaffoldModelRequest(InputArtifact template, bool withMarkdownStubs = false)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
        WithMarkdownStubs = withMarkdownStubs;
    }

    /// <summary>Gets the DOCX template input.</summary>
    public InputArtifact Template { get; }

    /// <summary>Gets whether Markdown placeholder files should be generated.</summary>
    public bool WithMarkdownStubs { get; }
}
