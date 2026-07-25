namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Inputs for deterministic template-specific schema generation.</summary>
public sealed record GenerateSchemaRequest
{
    /// <summary>Initializes a schema-generation request.</summary>
    public GenerateSchemaRequest(
        InputArtifact template,
        string? templateId = null,
        string? templateVersion = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (templateId is not null)
        {
            ValidateIdentityPart(templateId, nameof(templateId), 128);
        }

        if (templateVersion is not null)
        {
            ValidateIdentityPart(templateVersion, nameof(templateVersion), 64);
        }

        Template = template;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
    }

    /// <summary>Gets the placeholder-bearing DOCX template.</summary>
    public InputArtifact Template { get; }

    /// <summary>Gets an optional explicit template identifier.</summary>
    public string? TemplateId { get; }

    /// <summary>Gets an optional explicit template version.</summary>
    public string? TemplateVersion { get; }

    private static void ValidateIdentityPart(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maxLength
            || !IsAsciiLetterOrDigit(value[0])
            || value.Any(
                character =>
                    !IsAsciiLetterOrDigit(character)
                    && character is not ('.' or '_' or '-')))
        {
            throw new ArgumentException(
                $"{parameterName} must start with an ASCII letter or digit and "
                + $"contain at most {maxLength} ASCII letters, digits, '.', '_' "
                + "or '-'.",
                parameterName);
        }
    }

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9';
}
