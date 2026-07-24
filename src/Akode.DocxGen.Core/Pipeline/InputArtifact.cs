namespace Akode.DocxGen.Core.Pipeline;

/// <summary>
/// A named readable input whose stream lifetime remains owned by the caller.
/// </summary>
public sealed record InputArtifact
{
    /// <summary>Initializes a named readable input.</summary>
    public InputArtifact(string name, Stream content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new ArgumentException("The input stream must be readable.", nameof(content));
        }

        Name = name;
        Content = content;
    }

    /// <summary>Gets the source name used in diagnostics and relative resolution.</summary>
    public string Name { get; }

    /// <summary>Gets the readable content stream.</summary>
    public Stream Content { get; }
}
