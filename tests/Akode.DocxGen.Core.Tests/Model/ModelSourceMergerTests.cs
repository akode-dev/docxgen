using System.Text.Json;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Model;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Model;

public sealed class ModelSourceMergerTests
{
    [Fact]
    public void BuildsNestedMarkdownAndTableValuesFromAnchors()
    {
        const string markdown =
            """
            <!-- docxgen:section ds.Document.Introduction -->
            A **short** introduction.

            <!-- docxgen:section Team format=table columns=Name,Role -->
            | Name | Role |
            |---|---|
            | Alexei | Architect |
            """;
        var anchors = SectionAnchorParser.Parse(markdown);

        var result = ModelSourceMerger.Merge(null, anchors);

        result.IsValid.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        var dataSource = result.Data["ds"].ShouldBeOfType<ObjectModelValue>();
        var document = dataSource.Properties["Document"]
            .ShouldBeOfType<ObjectModelValue>();
        document.Properties["Introduction"].ShouldBe(
            new InlineMarkdownModelValue("A **short** introduction."));

        var team = dataSource.Properties["Team"]
            .ShouldBeOfType<CollectionModelValue>();
        team.Items.ShouldHaveSingleItem()
            .ShouldBeOfType<ObjectModelValue>()
            .Properties["Role"]
            .ShouldBe(new PlainTextModelValue("Architect"));
    }

    [Fact]
    public void DeepMergesModelOverMarkdownAndReportsOnlyConflictingLeaf()
    {
        const string markdown =
            """
            <!-- docxgen:section Body -->
            Markdown body.
            <!-- docxgen:section Approach -->
            Markdown approach.
            """;
        var modelData = new Dictionary<string, ModelValue>(StringComparer.Ordinal)
        {
            ["ds"] = new ObjectModelValue(
                new Dictionary<string, ModelValue>(StringComparer.Ordinal)
                {
                    ["Title"] = StringValue("Proposal"),
                    ["Body"] = new PlainTextModelValue("Explicit body"),
                }),
        };

        var result = ModelSourceMerger.Merge(
            modelData,
            SectionAnchorParser.Parse(markdown));

        result.IsValid.ShouldBeTrue();
        var warning = result.Diagnostics.ShouldHaveSingleItem();
        warning.Code.ShouldBe(DiagnosticCode.ModelOverridesMarkdown);
        warning.Path.ShouldBe("/data/ds/Body");
        warning.Hint.ShouldContain("model JSON");

        var dataSource = result.Data["ds"].ShouldBeOfType<ObjectModelValue>();
        dataSource.Properties["Title"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetString()
            .ShouldBe("Proposal");
        dataSource.Properties["Body"].ShouldBe(
            new PlainTextModelValue("Explicit body"));
        dataSource.Properties["Approach"].ShouldBe(
            new InlineMarkdownModelValue("Markdown approach."));
    }

    [Fact]
    public void ModelScalarOverridesAnEntireAnchoredSubtree()
    {
        const string markdown =
            """
            <!-- docxgen:section ds.Section.Body -->
            Body.
            <!-- docxgen:section ds.Section.Notes -->
            Notes.
            """;
        var modelData = new Dictionary<string, ModelValue>(StringComparer.Ordinal)
        {
            ["ds"] = new ObjectModelValue(
                new Dictionary<string, ModelValue>(StringComparer.Ordinal)
                {
                    ["Section"] = StringValue("Replacement"),
                }),
        };

        var result = ModelSourceMerger.Merge(
            modelData,
            SectionAnchorParser.Parse(markdown));

        result.Diagnostics.ShouldHaveSingleItem().Path.ShouldBe("/data/ds/Section");
        result.Data["ds"]
            .ShouldBeOfType<ObjectModelValue>()
            .Properties["Section"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetString()
            .ShouldBe("Replacement");
    }

    [Fact]
    public void AppliesTypedOverridesAtHighestPrecedence()
    {
        const string markdown =
            """
            <!-- docxgen:section Body -->
            Markdown body.
            """;
        var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Body"] = "Replacement",
            ["ds.Count"] = "12",
            ["ds.Ratio"] = "1.5e2",
            ["ds.Enabled"] = "TRUE",
            ["ds.Optional"] = "null",
            ["ds.Code"] = "@0042",
        };

        var result = ModelSourceMerger.Merge(
            null,
            SectionAnchorParser.Parse(markdown),
            overrides);

        result.IsValid.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        var dataSource = result.Data["ds"].ShouldBeOfType<ObjectModelValue>();
        dataSource.Properties["Body"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetString()
            .ShouldBe("Replacement");
        dataSource.Properties["Count"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetInt32()
            .ShouldBe(12);
        dataSource.Properties["Ratio"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetDouble()
            .ShouldBe(150);
        dataSource.Properties["Enabled"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetBoolean()
            .ShouldBeTrue();
        dataSource.Properties["Optional"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.ValueKind
            .ShouldBe(JsonValueKind.Null);
        dataSource.Properties["Code"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetString()
            .ShouldBe("0042");
    }

    [Fact]
    public void RejectsOverlappingAnchorPathsWithoutLastWriterWins()
    {
        const string markdown =
            """
            <!-- docxgen:section Section -->
            Parent.
            <!-- docxgen:section ds.Section.Child -->
            Child.
            """;

        var result = ModelSourceMerger.Merge(
            null,
            SectionAnchorParser.Parse(markdown));

        result.IsValid.ShouldBeFalse();
        result.Data["ds"]
            .ShouldBeOfType<ObjectModelValue>()
            .Properties["Section"]
            .ShouldBe(new InlineMarkdownModelValue("Parent."));
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.ModelDuplicateSection);
        diagnostic.Path.ShouldBe("/data/ds/Section/Child");
    }

    [Fact]
    public void PropagatesAnchorAndTableDiagnostics()
    {
        const string markdown =
            """
            Preamble.
            <!-- docxgen:section Team format=table -->
            Not a table.
            """;

        var result = ModelSourceMerger.Merge(
            null,
            SectionAnchorParser.Parse(markdown));

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldBe(
        [
            DiagnosticCode.MarkdownAnchorPreambleIgnored,
            DiagnosticCode.MarkdownInvalidTableAnchor,
        ]);
    }

    [Fact]
    public void ReportsInvalidOverridePathAsModelDiagnostic()
    {
        var result = ModelSourceMerger.Merge(
            null,
            SectionAnchorParser.Parse(string.Empty),
            new Dictionary<string, string>
            {
                ["invalid-path"] = "value",
            });

        result.IsValid.ShouldBeFalse();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.ModelSchemaViolation);
        diagnostic.Path.ShouldBe("/overrides/invalid-path");
    }

    private static PrimitiveModelValue StringValue(string value) =>
        new(JsonSerializer.SerializeToElement(value));
}
