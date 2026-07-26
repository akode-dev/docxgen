using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Markdown;

public sealed class MarkdownContentParserTests
{
    [Fact]
    public void ParsesRepresentativeMarkdownAndCollectsStableStatistics()
    {
        const string markdown =
            """
            # Architecture

            ![Pipeline](architecture.svg "System flow")

            | Phase | Outcome |
            | --- | --- |
            | P1 | CLI |

            ```csharp
            Console.WriteLine("DOCX");
            ```
            """;

        var content = MarkdownContentParser.Parse(
            markdown,
            DefaultOptions with { HeadingOffset = 1 },
            new StubAssetResolver());

        content.Stats.ShouldBe(
            new MarkdownStats(
                Sections: 1,
                Headings: 1,
                Tables: 1,
                Images: 1,
                CodeBlocks: 1));
        content.Blocks[0].ShouldBeOfType<MarkdownHeadingNode>()
            .Level.ShouldBe(2);
        content.Blocks.ShouldContain(block => block is MarkdownTableNode);
        content.Blocks.ShouldContain(block => block is MarkdownCodeBlockNode);
        content.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void AppliesSecurityPolicyAndReportsMarkdownDowngrades()
    {
        const string markdown =
            """
            ###### Too deep

            <span>raw</span>

            ![Remote](https://example.test/image.png)
            """;

        var content = MarkdownContentParser.Parse(
            markdown,
            DefaultOptions with { HeadingOffset = 2 },
            new StubAssetResolver());

        content.Blocks[0].ShouldBeOfType<MarkdownHeadingNode>()
            .Level.ShouldBe(6);
        content.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.HeadingLevelClamped);
        content.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.RawHtmlStripped);
        content.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.RemoteImageBlocked);
        content.Stats.Images.ShouldBe(1);
    }

    [Fact]
    public void ResolvesARemoteImageOnlyWhenExplicitlyAllowed()
    {
        var content = MarkdownContentParser.Parse(
            "![Remote](https://cdn.example.test/image.png)",
            DefaultOptions with { AllowRemoteImages = true },
            new StubAssetResolver());

        var paragraph = content.Blocks.ShouldHaveSingleItem()
            .ShouldBeOfType<MarkdownParagraphNode>();
        paragraph.Inlines.ShouldHaveSingleItem()
            .ShouldBeOfType<MarkdownImageNode>()
            .Asset.FullPath.ShouldBe("https://cdn.example.test/image.png");
        content.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void PreservesTaskListMarkersWithoutDowngradeWarnings()
    {
        var content = MarkdownContentParser.Parse(
            """
            - [x] Completed
            - [ ] Pending
            """,
            DefaultOptions,
            new StubAssetResolver());

        var list = content.Blocks.ShouldHaveSingleItem()
            .ShouldBeOfType<MarkdownListNode>();
        var completed = list.Items[0].Blocks.ShouldHaveSingleItem()
            .ShouldBeOfType<MarkdownParagraphNode>();
        completed.Inlines[0].ShouldBe(new MarkdownTaskListNode(Checked: true));
        var pending = list.Items[1].Blocks.ShouldHaveSingleItem()
            .ShouldBeOfType<MarkdownParagraphNode>();
        pending.Inlines[0].ShouldBe(new MarkdownTaskListNode(Checked: false));
        content.Diagnostics.ShouldBeEmpty();
    }

    private static RenderOptions DefaultOptions { get; } = new()
    {
        Culture = "en-US",
        Strict = true,
    };

    private sealed class StubAssetResolver : IAssetResolver, IRemoteAssetResolver
    {
        public ResolvedAsset Resolve(string relativePath, AssetKind kind) =>
            new(
                Path.GetFullPath(relativePath),
                kind,
                "image/svg+xml",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray());

        public ResolvedAsset ResolveRemote(Uri uri, AssetKind kind) =>
            new(
                uri.AbsoluteUri,
                kind,
                "image/png",
                new byte[] { 137, 80, 78, 71 });
    }
}
