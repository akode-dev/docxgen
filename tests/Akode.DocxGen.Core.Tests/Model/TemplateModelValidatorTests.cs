using System.Text;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Model;

public sealed class TemplateModelValidatorTests
{
    [Fact]
    public void AcceptsTheReferenceModelAgainstTheAdjacentTemplateSchema()
    {
        var modelPath = Path.Combine(
            RepositoryLayout.Root,
            "samples",
            "model.json");
        var templatePath = Path.Combine(
            RepositoryLayout.Root,
            "templates",
            "proposal.docx");

        var diagnostics = TemplateModelValidator.Validate(
            File.ReadAllBytes(modelPath),
            templatePath);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsTheExactPathForAReferenceModelContractViolation()
    {
        var modelPath = Path.Combine(
            RepositoryLayout.Root,
            "samples",
            "model.json");
        var templatePath = Path.Combine(
            RepositoryLayout.Root,
            "templates",
            "proposal.docx");
        var json = File.ReadAllText(modelPath)
            .Replace(
                "\"Title\": \"Customer Platform Proposal\"",
                "\"Title\": \"\"",
                StringComparison.Ordinal);

        var diagnostics = TemplateModelValidator.Validate(
            Encoding.UTF8.GetBytes(json),
            templatePath);

        diagnostics.Any(
            diagnostic =>
                diagnostic.Code == DiagnosticCode.ModelSchemaViolation
                && diagnostic.Path?.Contains(
                    "/data/ds/Document/Title",
                    StringComparison.Ordinal) == true)
            .ShouldBeTrue();
    }
}
