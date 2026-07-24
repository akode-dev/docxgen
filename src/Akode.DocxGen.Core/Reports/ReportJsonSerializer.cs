using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Reports;

/// <summary>Serializes version 1.0 command reports with stable JSON conventions.</summary>
public static class ReportJsonSerializer
{
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(
        writeIndented: false);
    private static readonly JsonSerializerOptions IndentedOptions = CreateOptions(
        writeIndented: true);

    /// <summary>Serializes a command report for stdout.</summary>
    public static string Serialize<TData>(
        CommandReport<TData> report,
        bool writeIndented = false)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(
            report,
            writeIndented ? IndentedOptions : CompactOptions);
    }

    private static JsonSerializerOptions CreateOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = writeIndented,
        };
        options.Converters.Add(
            new JsonStringEnumConverter<DiagnosticSeverity>(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));
        options.Converters.Add(
            new JsonStringEnumConverter<ModelValueKind>(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));
        options.MakeReadOnly();
        return options;
    }
}
