using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Markdown;

public sealed class SectionAnchorParserTests
{
    [Fact]
    public void ParsesSectionsAttributesAndExplicitEndInSourceOrder()
    {
        const string markdown =
            """
            <!-- DOCXGEN:SECTION ExecutiveSummary -->

            Akode proposes a **platform**.

            <!-- docxgen:section ds.Approach format=markdown audience=technical -->

            ## Delivery approach

            1. Discovery
            2. Foundation

            <!-- docxgen:end -->

            This text is outside the anchored region.
            """;

        var result = SectionAnchorParser.Parse(markdown);

        result.IsValid.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        result.Sections.Select(section => section.Path).ShouldBe(
        [
            "ds.ExecutiveSummary",
            "ds.Approach",
        ]);
        result.Sections[0].Markdown.ShouldBe("Akode proposes a **platform**.");
        result.Sections[1].Markdown.ShouldBe(
            """
            ## Delivery approach

            1. Discovery
            2. Foundation
            """);
        result.Sections[1].Attributes["FORMAT"].ShouldBe("markdown");
        result.Sections[1].Attributes["audience"].ShouldBe("technical");
    }

    [Fact]
    public void AcceptsBomCrlfAndTrailingMarkerWhitespace()
    {
        var markdown =
            "\uFEFF<!-- docxgen:section Body -->   \r\n\r\nFirst\r\nSecond\r\n";

        var result = SectionAnchorParser.Parse(markdown);

        var section = result.Sections.ShouldHaveSingleItem();
        section.Path.ShouldBe("ds.Body");
        section.Markdown.ShouldBe("First\nSecond");
        section.AnchorLine.ShouldBe(1);
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void IgnoresMarkerShapedTextInsideFencesAndInlineCode()
    {
        const string markdown =
            """
            <!-- docxgen:section Body -->

            `<!-- docxgen:section Inline -->`

            ````markdown
            <!-- docxgen:section Fenced -->
            <!-- docxgen:end -->
            ````

            Still body.

            <!-- docxgen:section Actual -->
            Actual section.
            """;

        var result = SectionAnchorParser.Parse(markdown);

        result.Sections.Select(section => section.Path).ShouldBe(
        [
            "ds.Body",
            "ds.Actual",
        ]);
        result.Sections[0].Markdown.ShouldContain("docxgen:section Inline");
        result.Sections[0].Markdown.ShouldContain("docxgen:section Fenced");
        result.Sections[0].Markdown.ShouldContain("Still body.");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void ReportsPreambleOnlyWhenAnchoredSectionsExist()
    {
        const string anchored =
            """
            # Forgotten heading

            <!-- docxgen:section Body -->
            Body.
            """;

        var anchoredResult = SectionAnchorParser.Parse(anchored);
        var diagnostic = anchoredResult.Diagnostics.ShouldHaveSingleItem();

        diagnostic.Code.ShouldBe(DiagnosticCode.MarkdownAnchorPreambleIgnored);
        diagnostic.Path.ShouldBe("/markdown/lines/1");
        anchoredResult.IsValid.ShouldBeTrue();

        var plainResult = SectionAnchorParser.Parse("# Standalone body");
        plainResult.Sections.ShouldBeEmpty();
        plainResult.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void RejectsDuplicateResolvedPathAndKeepsFirstSection()
    {
        const string markdown =
            """
            <!-- docxgen:section Body -->
            First.

            <!-- docxgen:section ds.Body -->
            Second.
            """;

        var result = SectionAnchorParser.Parse(markdown);

        result.IsValid.ShouldBeFalse();
        result.Sections.ShouldHaveSingleItem().Markdown.ShouldBe("First.");
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.ModelDuplicateSection);
        diagnostic.Path.ShouldBe("/data/ds/Body");
        diagnostic.Message.ShouldContain("line 4");
    }

    [Theory]
    [InlineData("<!-- docxgen:section invalid-name -->")]
    [InlineData("<!-- docxgen:section Body format=table FORMAT=markdown -->")]
    [InlineData("<!-- docxgen:section -->")]
    public void RejectsMalformedDocxGenMarkers(string marker)
    {
        var result = SectionAnchorParser.Parse(marker);

        result.IsValid.ShouldBeFalse();
        result.Sections.ShouldBeEmpty();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.MarkdownInvalidSectionAnchor);
        diagnostic.Path.ShouldBe("/markdown/lines/1");
    }

    [Fact]
    public void RejectsEndMarkerWithoutAnOpenSection()
    {
        var result = SectionAnchorParser.Parse("<!-- docxgen:end -->");

        result.IsValid.ShouldBeFalse();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.MarkdownInvalidSectionAnchor);
        diagnostic.Message.ShouldContain("no open section");
    }

    [Fact]
    public void PreservesCaseSensitiveNamesAsDistinctPaths()
    {
        const string markdown =
            """
            <!-- docxgen:section Approach -->
            Upper.
            <!-- docxgen:section approach -->
            Lower.
            """;

        var result = SectionAnchorParser.Parse(markdown);

        result.IsValid.ShouldBeTrue();
        result.Sections.Select(section => section.Path).ShouldBe(
        [
            "ds.Approach",
            "ds.approach",
        ]);
    }

    [Fact]
    public void SupportsAValidatedCustomDefaultRoot()
    {
        var result = SectionAnchorParser.Parse(
            "<!-- docxgen:section Body -->\nText.",
            "content");

        result.Sections.ShouldHaveSingleItem().Path.ShouldBe("content.Body");
        Should.Throw<ArgumentException>(
            () => SectionAnchorParser.Parse(string.Empty, "invalid.root"));
    }
}
