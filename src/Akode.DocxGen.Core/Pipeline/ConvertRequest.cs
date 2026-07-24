namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for template-less Markdown conversion.</summary>
public sealed record ConvertRequest
{
    /// <summary>Initializes a conversion request.</summary>
    public ConvertRequest(
        InputArtifact markdown,
        InputArtifact? styleReference = null,
        int headingOffset = 0,
        bool includeToc = false,
        bool validateOutput = false)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (headingOffset is < -5 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(headingOffset),
                headingOffset,
                "Heading offset must be between -5 and 5.");
        }

        Markdown = markdown;
        StyleReference = styleReference;
        HeadingOffset = headingOffset;
        IncludeToc = includeToc;
        ValidateOutput = validateOutput;
    }

    /// <summary>Gets the Markdown input.</summary>
    public InputArtifact Markdown { get; }

    /// <summary>Gets an optional DOCX style reference.</summary>
    public InputArtifact? StyleReference { get; }

    /// <summary>Gets the heading-level offset.</summary>
    public int HeadingOffset { get; }

    /// <summary>Gets whether the draft should contain a TOC field.</summary>
    public bool IncludeToc { get; }

    /// <summary>Gets whether output OOXML validation is requested.</summary>
    public bool ValidateOutput { get; }
}
