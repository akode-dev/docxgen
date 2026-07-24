using System.Globalization;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using DocxTemplater;

namespace Akode.DocxGen.Docx.Rendering;

/// <summary>Binds template values and renders bounded Markdown into Open XML.</summary>
public sealed class DocxTemplaterRenderer : IDocumentRenderer
{
    /// <inheritdoc />
    public async Task<RenderOutcome> RenderAsync(
        Stream templateDocument,
        BoundModel model,
        RenderOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateDocument);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        var settings = new ProcessSettings
        {
            Culture = CultureInfo.GetCultureInfo(options.Culture),
            IgnoreLineBreaksAroundTags = true,
            BindingErrorHandling = options.Strict
                ? BindingErrorHandling.ThrowException
                : BindingErrorHandling.SkipBindingAndRemoveContent,
        };

        using var template = new DocxTemplate(templateDocument, settings);
        template.RegisterFormatter(new DocxGenMarkdownFormatter());
        template.RegisterFormatter(new DocxGenImageFormatter());
        foreach (var root in model.Roots)
        {
            template.BindModel(root.Key, root.Value ?? string.Empty);
        }

        var processed = template.Process();
        var output = new MemoryStream();
        processed.Position = 0;
        await processed.CopyToAsync(
            output,
            cancellationToken).ConfigureAwait(false);
        output.Position = 0;
        return new RenderOutcome(
            output,
            model.BoundPaths,
            model.UnboundPaths,
            model.MarkdownStats,
            []);
    }
}
