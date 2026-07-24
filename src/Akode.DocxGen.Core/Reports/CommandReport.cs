using System.Collections.ObjectModel;
using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Reports;

/// <summary>
/// Versioned machine-readable operation envelope shared by CLI and future MCP
/// projections.
/// </summary>
/// <typeparam name="TData">Command-specific success data.</typeparam>
public sealed record CommandReport<TData>
    where TData : class
{
    internal CommandReport(
        string command,
        bool ok,
        ExitCode exitCode,
        string? errorCode,
        string message,
        string? hint,
        IReadOnlyList<Diagnostic> diagnostics,
        TData? data)
    {
        ReportVersion = ReportContract.Version;
        Command = command;
        Ok = ok;
        ExitCode = exitCode;
        ErrorCode = errorCode;
        Message = message;
        Hint = hint;
        Diagnostics = diagnostics;
        Data = data;
    }

    /// <summary>Gets the report schema version.</summary>
    public string ReportVersion { get; }

    /// <summary>Gets the stable command name.</summary>
    public string Command { get; }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Ok { get; }

    /// <summary>Gets the numeric process exit code.</summary>
    public ExitCode ExitCode { get; }

    /// <summary>Gets the primary diagnostic code when the operation failed.</summary>
    public string? ErrorCode { get; }

    /// <summary>Gets the concise operation summary.</summary>
    public string Message { get; }

    /// <summary>Gets the primary remediation when the operation failed.</summary>
    public string? Hint { get; }

    /// <summary>Gets ordered diagnostics produced by the operation.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets command-specific success data, or null on failure.</summary>
    public TData? Data { get; }

}

/// <summary>Creates invariant-preserving machine-readable command reports.</summary>
public static class CommandReport
{
    /// <summary>Creates a successful report containing no error diagnostics.</summary>
    public static CommandReport<TData> Success<TData>(
        string command,
        string message,
        TData data,
        IEnumerable<Diagnostic>? diagnostics = null)
        where TData : class
    {
        ValidateCommandAndMessage(command, message);
        ArgumentNullException.ThrowIfNull(data);
        var diagnosticList = CopyDiagnostics(diagnostics);
        if (diagnosticList.Any(
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new ArgumentException(
                "A successful report cannot contain error diagnostics.",
                nameof(diagnostics));
        }

        return new CommandReport<TData>(
            command,
            ok: true,
            ExitCode.Success,
            errorCode: null,
            message,
            hint: null,
            diagnosticList,
            data);
    }

    /// <summary>Creates a failed report from an ordered diagnostic collection.</summary>
    public static CommandReport<TData> Failure<TData>(
        string command,
        ExitCode exitCode,
        string message,
        IEnumerable<Diagnostic> diagnostics)
        where TData : class
    {
        ValidateCommandAndMessage(command, message);
        if (exitCode == ExitCode.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exitCode),
                exitCode,
                "A failed report requires a non-zero exit code.");
        }

        var diagnosticList = CopyDiagnostics(diagnostics);
        var primary = diagnosticList.FirstOrDefault(
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            ?? throw new ArgumentException(
                "A failed report requires at least one error diagnostic.",
                nameof(diagnostics));

        return new CommandReport<TData>(
            command,
            ok: false,
            exitCode,
            primary.Code,
            message,
            primary.Hint,
            diagnosticList,
            data: null);
    }

    private static void ValidateCommandAndMessage(string command, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        if (!ReportContract.Commands.Contains(command))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command,
                "The command is not part of report contract 1.0.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }

    private static ReadOnlyCollection<Diagnostic> CopyDiagnostics(
        IEnumerable<Diagnostic>? diagnostics)
    {
        var copy = diagnostics?.ToArray() ?? [];
        if (copy.Any(diagnostic => diagnostic is null))
        {
            throw new ArgumentException(
                "Diagnostics cannot contain null entries.",
                nameof(diagnostics));
        }

        return new ReadOnlyCollection<Diagnostic>(copy);
    }
}
