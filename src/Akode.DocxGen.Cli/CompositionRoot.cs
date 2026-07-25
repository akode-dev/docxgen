using Microsoft.Extensions.DependencyInjection;

namespace Akode.DocxGen.Cli;

internal static class CompositionRoot
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton(DocxGenPipelineFactory.CreatePipeline());
        return services.BuildServiceProvider();
    }
}
