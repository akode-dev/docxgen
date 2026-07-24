using Akode.DocxGen.Core.Model;

namespace Akode.DocxGen.Core.Abstractions;

/// <summary>Inspects the public data contract exposed by a DOCX template.</summary>
public interface ITemplateInspector
{
    /// <summary>Returns the placeholders and diagnostics discovered in a template.</summary>
    TemplateSchema Inspect(
        Stream templateDocument,
        CancellationToken cancellationToken = default);
}
