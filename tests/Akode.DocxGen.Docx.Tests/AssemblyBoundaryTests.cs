using Shouldly;
using Xunit;

namespace Akode.DocxGen.Docx.Tests;

public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void AdapterAssemblyHasAStableMarker()
    {
        typeof(DocxAdapterMarker).Assembly.GetName().Name.ShouldBe("Akode.DocxGen.Docx");
    }
}
