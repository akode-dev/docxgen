using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Model;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Markdown;

public sealed class AnchoredTableConverterTests
{
    [Fact]
    public void ConvertsGfmRowsToCaseSensitiveObjectProperties()
    {
        const string markdown =
            """
            <!-- docxgen:section TeamTable format=table columns=Name,Role,Allocation -->

            | Name | Role | Allocation |
            |---|---|---:|
            | **Alexei** | `Solution Architect` | 0.5 |
            | N. N. | [Data Engineer](https://example.test/role) | 1.0 |
            """;
        var section = SectionAnchorParser.Parse(markdown)
            .Sections
            .ShouldHaveSingleItem();

        var result = AnchoredTableConverter.Convert(section);

        result.IsValid.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        var collection = result.Value.ShouldNotBeNull();
        collection.Items.Count.ShouldBe(2);

        var first = collection.Items[0].ShouldBeOfType<ObjectModelValue>();
        first.Properties.Keys.ShouldBe(["Name", "Role", "Allocation"]);
        first.Properties["Name"].ShouldBe(new PlainTextModelValue("Alexei"));
        first.Properties["Role"].ShouldBe(
            new PlainTextModelValue("Solution Architect"));
        first.Properties["Allocation"].ShouldBe(new PlainTextModelValue("0.5"));

        var second = collection.Items[1].ShouldBeOfType<ObjectModelValue>();
        second.Properties["Role"].ShouldBe(new PlainTextModelValue("Data Engineer"));
    }

    [Fact]
    public void ConvertsAnEmptyTableToAnEmptyCollection()
    {
        const string markdown =
            """
            <!-- docxgen:section Deliverables format=table columns=Name,Owner -->
            | Name | Owner |
            |---|---|
            """;
        var section = SectionAnchorParser.Parse(markdown)
            .Sections
            .ShouldHaveSingleItem();

        var result = AnchoredTableConverter.Convert(section);

        result.IsValid.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Items.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(
        "<!-- docxgen:section Team format=table -->\n| Name |\n|---|\n| A |")]
    [InlineData(
        "<!-- docxgen:section Team format=table columns=Name,Name -->\n| Name | Name |\n|---|---|\n| A | B |")]
    [InlineData(
        "<!-- docxgen:section Team format=table columns=Name -->\nProse, not a table.")]
    [InlineData(
        "<!-- docxgen:section Team format=markdown columns=Name -->\n| Name |\n|---|\n| A |")]
    public void RejectsInvalidTableContracts(string markdown)
    {
        var section = SectionAnchorParser.Parse(markdown)
            .Sections
            .ShouldHaveSingleItem();

        var result = AnchoredTableConverter.Convert(section);

        result.IsValid.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.MarkdownInvalidTableAnchor);
        diagnostic.Path.ShouldBe("/markdown/lines/1");
        diagnostic.Hint.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RejectsColumnCountThatDoesNotMatchTheHeader()
    {
        const string markdown =
            """
            <!-- docxgen:section Team format=table columns=Name,Role,Allocation -->
            | Name | Role |
            |---|---|
            | Alexei | Architect |
            """;
        var section = SectionAnchorParser.Parse(markdown)
            .Sections
            .ShouldHaveSingleItem();

        var result = AnchoredTableConverter.Convert(section);

        result.IsValid.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem().Message.ShouldContain(
            "header");
    }
}
