namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Technology-neutral input for template contract inspection.</summary>
public sealed record InspectRequest(
    InputArtifact Template,
    bool IncludeTextProbe = false);
