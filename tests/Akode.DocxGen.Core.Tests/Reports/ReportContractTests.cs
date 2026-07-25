using System.Text.Json;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Core.Reports;
using Json.Schema;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Reports;

public sealed class ReportContractTests
{
    private const string Hash =
        "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void SuccessReportUsesVersionedAgentFriendlyJson()
    {
        var warning = DiagnosticRegistry.Create(DiagnosticCode.TocRequiresWordUpdate);
        var data = new RenderReportData(
            Output: "out/proposal-v1.0.docx",
            OutputBytes: 1024,
            TemplateHash: Hash,
            ModelHash: Hash,
            DurationMs: 42,
            Bound: ["ds.Document.Title"],
            Unbound: [],
            MarkdownStats: new MarkdownStats(1, 2, 0, 0, 0),
            Validation: new DocumentValidationSummary(true, 0, 1),
            DryRun: false,
            DocumentVersion: "1.0");
        var report = CommandReport.Success(
            CommandName.Render,
            "Document rendered.",
            data,
            [warning]);

        using var json = JsonDocument.Parse(ReportJsonSerializer.Serialize(report));
        var root = json.RootElement;

        root.GetProperty("reportVersion").GetString().ShouldBe("1.0");
        root.GetProperty("command").GetString().ShouldBe("render");
        root.GetProperty("ok").GetBoolean().ShouldBeTrue();
        root.GetProperty("exitCode").GetInt32().ShouldBe(0);
        root.TryGetProperty("errorCode", out _).ShouldBeFalse();
        root.TryGetProperty("hint", out _).ShouldBeFalse();
        root.GetProperty("diagnostics")[0]
            .GetProperty("severity")
            .GetString()
            .ShouldBe("warning");
        root.GetProperty("data")
            .GetProperty("markdownStats")
            .GetProperty("headings")
            .GetInt32()
            .ShouldBe(2);
    }

    [Fact]
    public void FailureReportPromotesFirstErrorCodeAndHint()
    {
        var warning = DiagnosticRegistry.Create(DiagnosticCode.RawHtmlStripped);
        var error = DiagnosticRegistry.Create(
            DiagnosticCode.ModelSchemaViolation,
            "/data/ds/Document/Title");
        var report = CommandReport.Failure<ValidateModelReportData>(
            CommandName.ValidateModel,
            ExitCode.ModelError,
            "Model validation failed.",
            [warning, error]);

        using var json = JsonDocument.Parse(
            ReportJsonSerializer.Serialize(report, writeIndented: true));
        var root = json.RootElement;

        root.GetProperty("ok").GetBoolean().ShouldBeFalse();
        root.GetProperty("exitCode").GetInt32().ShouldBe(4);
        root.GetProperty("errorCode").GetString().ShouldBe(error.Code);
        root.GetProperty("hint").GetString().ShouldBe(error.Hint);
        root.TryGetProperty("data", out _).ShouldBeFalse();
        root.GetProperty("diagnostics").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public void ExtractReportSerializesAssetsAndSemanticStats()
    {
        var data = new ExtractReportData(
            "out/document.md",
            512,
            "out/document.assets",
            ["out/document.assets/image-001.png"],
            17,
            new DocxExtractionStats(8, 2, 3, 1, 1));
        var report = CommandReport.Success(
            CommandName.Extract,
            "DOCX extracted.",
            data);

        var serialized = ReportJsonSerializer.Serialize(report);
        using var json = JsonDocument.Parse(serialized);
        var root = json.RootElement;

        root.GetProperty("command").GetString().ShouldBe("extract");
        root.GetProperty("data")
            .GetProperty("assets")
            .GetArrayLength()
            .ShouldBe(1);
        root.GetProperty("data")
            .GetProperty("stats")
            .GetProperty("listItems")
            .GetInt32()
            .ShouldBe(3);
        var schemaPath = Path.Combine(
            RepositoryLayout.Root,
            "docs",
            "schemas",
            "docxgen-report-1.0.schema.json");
        var schema = JsonSchema.FromText(File.ReadAllText(schemaPath));
        schema.Evaluate(
                json.RootElement,
                new EvaluationOptions
                {
                    OutputFormat = OutputFormat.List,
                })
            .IsValid
            .ShouldBeTrue();
    }

    [Fact]
    public void GenerateSchemaReportSerializesAndValidates()
    {
        var data = new GenerateSchemaReportData(
            "out/proposal.schema.json",
            4096,
            "proposal",
            "2.1.0",
            Hash,
            12,
            Checked: false,
            DurationMs: 7);
        var report = CommandReport.Success(
            CommandName.GenerateSchema,
            "Template schema generated.",
            data);

        var serialized = ReportJsonSerializer.Serialize(report);
        using var json = JsonDocument.Parse(serialized);
        var root = json.RootElement;

        root.GetProperty("command").GetString().ShouldBe("generate-schema");
        root.GetProperty("data")
            .GetProperty("bindingCount")
            .GetInt32()
            .ShouldBe(12);
    }

    [Fact]
    public void ReportFactoriesRejectContradictoryStates()
    {
        var error = DiagnosticRegistry.Create(DiagnosticCode.ModelInvalidJson);
        var data = new ValidateModelReportData(
            Hash,
            Hash,
            [],
            [],
            MarkdownStats.Empty,
            1);

        Should.Throw<ArgumentException>(
            () => CommandReport.Success(
                CommandName.ValidateModel,
                "Invalid success.",
                data,
                [error]));
        Should.Throw<ArgumentOutOfRangeException>(
            () => CommandReport.Failure<ValidateModelReportData>(
                CommandName.ValidateModel,
                ExitCode.Success,
                "Invalid failure.",
                [error]));
        Should.Throw<ArgumentOutOfRangeException>(
            () => CommandReport.Success("unknown", "Invalid command.", data));
    }

    [Fact]
    public void ReportSchemaIsVersionedAndEveryLocalReferenceResolves()
    {
        var path = Path.Combine(
            RepositoryLayout.Root,
            "docs",
            "schemas",
            "docxgen-report-1.0.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllText(path));
        var root = schema.RootElement;
        var definitions = root.GetProperty("$defs");

        definitions.GetProperty("baseEnvelope")
            .GetProperty("properties")
            .GetProperty("reportVersion")
            .GetProperty("const")
            .GetString()
            .ShouldBe(ReportContract.Version);
        root.GetProperty("oneOf").GetArrayLength().ShouldBe(9);
        ReportContract.Commands.ShouldContain(CommandName.Extract);
        var schemaCommands = definitions.GetProperty("baseEnvelope")
            .GetProperty("properties")
            .GetProperty("command")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        schemaCommands.ShouldBe(
            ReportContract.Commands.Order(StringComparer.Ordinal));

        foreach (var reference in FindReferences(root))
        {
            reference.ShouldStartWith("#/$defs/");
            var name = reference["#/$defs/".Length..];
            definitions.TryGetProperty(name, out _).ShouldBeTrue(
                $"Schema reference '{reference}' does not resolve.");
        }
    }

    private static IEnumerable<string> FindReferences(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("$ref"))
                {
                    yield return property.Value.GetString()
                        ?? throw new InvalidOperationException("A schema $ref is null.");
                }

                foreach (var child in FindReferences(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in FindReferences(item))
                {
                    yield return child;
                }
            }
        }
    }
}
