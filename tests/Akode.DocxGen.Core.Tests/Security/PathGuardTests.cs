using Akode.DocxGen.Core.Security;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Security;

public sealed class PathGuardTests
{
    [Fact]
    public void ResolvesAPathContainedByTheApprovedRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "docxgen-assets");

        var result = PathGuard.ResolveContained(root, "images/diagram.png");

        result.ShouldBe(
            Path.GetFullPath(Path.Combine(root, "images", "diagram.png")));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("../../outside.png")]
    public void RejectsTraversalOutsideTheApprovedRoot(string path)
    {
        Should.Throw<ArgumentException>(
            () => PathGuard.ResolveContained(
                Path.Combine(Path.GetTempPath(), "docxgen-assets"),
                path));
    }

    [Fact]
    public void RejectsRootedAssetPaths()
    {
        var rooted = Path.GetFullPath(
            Path.Combine(Path.GetTempPath(), "outside.png"));

        Should.Throw<ArgumentException>(
            () => PathGuard.ResolveContained(
                Path.Combine(Path.GetTempPath(), "docxgen-assets"),
                rooted));
    }
}
