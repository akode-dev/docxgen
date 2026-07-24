using Akode.DocxGen.Core.Diagnostics;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Diagnostics;

public sealed class ExitCodeTests
{
    [Fact]
    public void PublicExitCodesAreStable()
    {
        ((int)ExitCode.Success).ShouldBe(0);
        ((int)ExitCode.UnexpectedError).ShouldBe(1);
        ((int)ExitCode.UsageError).ShouldBe(2);
        ((int)ExitCode.TemplateError).ShouldBe(3);
        ((int)ExitCode.ModelError).ShouldBe(4);
        ((int)ExitCode.RenderError).ShouldBe(5);
        ((int)ExitCode.ValidationError).ShouldBe(6);
        ((int)ExitCode.IoError).ShouldBe(7);
    }
}
