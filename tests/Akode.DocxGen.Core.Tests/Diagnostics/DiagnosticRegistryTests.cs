using System.Reflection;
using System.Text.RegularExpressions;
using Akode.DocxGen.Core.Diagnostics;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Diagnostics;

public sealed class DiagnosticRegistryTests
{
    private static readonly Regex CodePattern = new(
        "^[EWI]-[A-Z]{2,3}-[0-9]{3}$",
        RegexOptions.CultureInvariant);

    [Fact]
    public void RegistryCoversEveryPublicCodeExactlyOnce()
    {
        var constants = typeof(DiagnosticCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var registered = DiagnosticRegistry.All
            .Select(descriptor => descriptor.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        registered.ShouldBe(constants);
        registered.Distinct(StringComparer.Ordinal).Count().ShouldBe(registered.Length);
    }

    [Fact]
    public void EveryDescriptorHasValidCodeSeverityMessageAndHint()
    {
        foreach (var descriptor in DiagnosticRegistry.All)
        {
            CodePattern.IsMatch(descriptor.Code).ShouldBeTrue(
                $"Diagnostic code '{descriptor.Code}' does not follow the public format.");
            descriptor.Severity.ShouldBe(ExpectedSeverity(descriptor.Code));
            string.IsNullOrWhiteSpace(descriptor.DefaultMessage).ShouldBeFalse();
            string.IsNullOrWhiteSpace(descriptor.DefaultHint).ShouldBeFalse();
        }
    }

    [Fact]
    public void CreateUsesRegisteredDefaultsAndPath()
    {
        var diagnostic = DiagnosticRegistry.Create(
            DiagnosticCode.ModelSchemaViolation,
            "/data/ds/Document/Title");
        var descriptor = DiagnosticRegistry.Get(DiagnosticCode.ModelSchemaViolation);

        diagnostic.Code.ShouldBe(descriptor.Code);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldBe(descriptor.DefaultMessage);
        diagnostic.Hint.ShouldBe(descriptor.DefaultHint);
        diagnostic.Path.ShouldBe("/data/ds/Document/Title");
    }

    [Fact]
    public void CreateAllowsContextSpecificMessageAndHint()
    {
        var diagnostic = DiagnosticRegistry.Create(
            DiagnosticCode.AssetTooLarge,
            "/assets/diagram.png",
            "The asset is 12 MB; the limit is 10 MB.",
            "Compress diagram.png below 10 MB.");

        diagnostic.Message.ShouldBe("The asset is 12 MB; the limit is 10 MB.");
        diagnostic.Hint.ShouldBe("Compress diagram.png below 10 MB.");
    }

    [Fact]
    public void UnknownCodeCannotCreateDiagnostic()
    {
        var exception = Should.Throw<KeyNotFoundException>(
            () => DiagnosticRegistry.Create("E-UNK-999"));

        exception.Message.ShouldContain("E-UNK-999");
        DiagnosticRegistry.TryGet("E-UNK-999", out _).ShouldBeFalse();
    }

    [Fact]
    public void CollectorPreservesOrderAndReportsErrors()
    {
        var collector = new DiagnosticCollector();

        collector.Add(DiagnosticCode.RawHtmlStripped, "/body/0");
        var error = collector.Add(DiagnosticCode.ModelInvalidJson, "/");

        collector.Items.Select(diagnostic => diagnostic.Code).ShouldBe(
        [
            DiagnosticCode.RawHtmlStripped,
            DiagnosticCode.ModelInvalidJson,
        ]);
        collector.HasErrors.ShouldBeTrue();
        error.Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    private static DiagnosticSeverity ExpectedSeverity(string code) =>
        code[0] switch
        {
            'E' => DiagnosticSeverity.Error,
            'W' => DiagnosticSeverity.Warning,
            'I' => DiagnosticSeverity.Information,
            _ => throw new InvalidOperationException($"Unknown diagnostic prefix: {code}"),
        };
}
