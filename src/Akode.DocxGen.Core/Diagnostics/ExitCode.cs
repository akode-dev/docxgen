namespace Akode.DocxGen.Core.Diagnostics;

/// <summary>Stable process exit codes exposed by the CLI.</summary>
public enum ExitCode
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,

    /// <summary>An unhandled product defect occurred.</summary>
    UnexpectedError = 1,

    /// <summary>Command-line usage or an input path is invalid.</summary>
    UsageError = 2,

    /// <summary>The template cannot be inspected or processed.</summary>
    TemplateError = 3,

    /// <summary>The data model is invalid or incomplete.</summary>
    ModelError = 4,

    /// <summary>The renderer failed to produce a document.</summary>
    RenderError = 5,

    /// <summary>The generated package failed validation.</summary>
    ValidationError = 6,

    /// <summary>A file-system operation failed.</summary>
    IoError = 7,
}
