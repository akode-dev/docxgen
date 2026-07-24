using System.Collections.Frozen;

namespace Akode.DocxGen.Core.Reports;

/// <summary>Stable names and version of the machine-readable report contract.</summary>
public static class ReportContract
{
    /// <summary>Current JSON report schema version.</summary>
    public const string Version = "1.0";

    /// <summary>Supported command names in the version 1.0 envelope.</summary>
    public static IReadOnlySet<string> Commands { get; } =
        new[]
        {
            CommandName.Convert,
            CommandName.Inspect,
            CommandName.Render,
            CommandName.ScaffoldModel,
            CommandName.Validate,
            CommandName.ValidateModel,
        }.ToFrozenSet(StringComparer.Ordinal);
}

/// <summary>Stable command names written to machine-readable reports.</summary>
public static class CommandName
{
    /// <summary>The template inspection command.</summary>
    public const string Inspect = "inspect";

    /// <summary>The model scaffolding command.</summary>
    public const string ScaffoldModel = "scaffold-model";

    /// <summary>The model validation command.</summary>
    public const string ValidateModel = "validate-model";

    /// <summary>The template rendering command.</summary>
    public const string Render = "render";

    /// <summary>The template-less conversion command.</summary>
    public const string Convert = "convert";

    /// <summary>The existing-document validation command.</summary>
    public const string Validate = "validate";
}
