using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Docx.Utilities;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Akode.DocxGen.Docx.PostProcessing;

/// <summary>Requests Word to refresh TOC and other fields when opening a file.</summary>
public sealed class UpdateFieldsPostProcessor : IDocumentPostProcessor
{
    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public void Apply(
        Stream document,
        PostProcessOptions options,
        DiagnosticCollector diagnostics)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (!options.UpdateFieldsOnOpen)
        {
            return;
        }

        using var package = WordprocessingDocument.Open(
            new NonClosingStream(document),
            true);
        var main = package.MainDocumentPart
            ?? throw new InvalidDataException("The document has no main part.");
        var settingsPart = main.DocumentSettingsPart
            ?? main.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings ??= new Settings();
        var update = settingsPart.Settings.GetFirstChild<UpdateFieldsOnOpen>();
        if (update is null)
        {
            settingsPart.Settings.AddChild(
                new UpdateFieldsOnOpen { Val = true },
                true);
        }
        else
        {
            update.Val = true;
        }

        settingsPart.Settings.Save();
        diagnostics.Add(DiagnosticCode.TocRequiresWordUpdate);
    }
}
