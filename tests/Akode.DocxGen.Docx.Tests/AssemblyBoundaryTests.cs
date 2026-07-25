using Akode.DocxGen.Core.Pipeline;
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

    [Fact]
    public void PublicFacadeCreatesTheDefaultPipeline()
    {
        Akode.DocxGen.DocxGenPipelineFactory.CreatePipeline()
            .ShouldBeOfType<DocxGenPipeline>();
    }
}
