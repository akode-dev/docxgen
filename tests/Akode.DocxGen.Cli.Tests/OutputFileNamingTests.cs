using Akode.DocxGen.Cli.Output;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Cli.Tests;

public sealed class OutputFileNamingTests
{
    [Theory]
    [InlineData("Proposal.docx", "1.2.0", "Proposal-v1.2.0.docx")]
    [InlineData("Proposal.docx", "v2 beta", "Proposal-v2-beta.docx")]
    [InlineData("Proposal-v1.0.docx", "1.0", "Proposal-v1.0.docx")]
    public void AppendsANormalizedVersionExactlyOnce(
        string path,
        string version,
        string expected)
    {
        OutputFileNaming.AppendVersion(path, version).ShouldBe(expected);
    }

    [Fact]
    public void PreservesTheOutputDirectory()
    {
        var result = OutputFileNaming.AppendVersion(
            Path.Combine("artifacts", "Proposal.docx"),
            "3.0");

        result.ShouldBe(Path.Combine("artifacts", "Proposal-v3.0.docx"));
    }
}
