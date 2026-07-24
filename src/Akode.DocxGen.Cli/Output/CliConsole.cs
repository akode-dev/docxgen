using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Reports;

namespace Akode.DocxGen.Cli.Output;

internal static class CliOutput
{
    public static Stream StandardInput() => Console.OpenStandardInput();

    public static void WriteReport<TData>(
        CommandReport<TData> report,
        bool json)
        where TData : class
    {
        if (json)
        {
            Console.Out.WriteLine(
                ReportJsonSerializer.Serialize(report, writeIndented: true));
            return;
        }

        var destination = report.Ok ? Console.Out : Console.Error;
        destination.WriteLine(report.Message);
        foreach (var diagnostic in report.Diagnostics)
        {
            destination.WriteLine(FormatDiagnostic(diagnostic));
        }
    }

    public static void WriteUsageError(string message)
    {
        Console.Error.WriteLine(message);
    }

    private static string FormatDiagnostic(Diagnostic diagnostic)
    {
        var location = diagnostic.Path is null
            ? string.Empty
            : $" [{diagnostic.Path}]";
        return $"{diagnostic.Code}{location}: {diagnostic.Message} Hint: {diagnostic.Hint}";
    }
}
