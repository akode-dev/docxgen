using System.Text;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Core.Security;
using Akode.DocxGen.Docx.PostProcessing;
using Akode.DocxGen.Docx.Rendering;
using Akode.DocxGen.Docx.Utilities;
using Akode.DocxGen.Docx.Validation;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Akode.DocxGen.Docx.Conversion;

/// <summary>Creates polished standalone DOCX documents from Markdown.</summary>
public sealed class DocxMarkdownConverter : IMarkdownDocumentConverter
{
    /// <inheritdoc />
    public async Task<ConvertResult> ConvertAsync(
        ConvertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var markdownBytes = await ReadAllAsync(
            request.Markdown.Content,
            cancellationToken).ConfigureAwait(false);
        if (markdownBytes.LongLength > Limits.Default.MaxMarkdownBytes)
        {
            throw new InvalidDataException(
                $"Markdown exceeds the {Limits.Default.MaxMarkdownBytes}-byte limit.");
        }

        var assetsRoot = ResolveAssetsRoot(request.Markdown.Name);
        var options = new RenderOptions
        {
            Culture = "en-US",
            Strict = true,
            HeadingOffset = request.HeadingOffset,
        };
        var content = MarkdownContentParser.Parse(
            Encoding.UTF8.GetString(markdownBytes),
            options,
            new LocalAssetResolver(assetsRoot));
        var output = request.StyleReference is null
            ? CreateBaseDocument()
            : await CopyStyleReferenceAsync(
                request.StyleReference.Content,
                cancellationToken).ConfigureAwait(false);

        using (var package = WordprocessingDocument.Open(
                   new NonClosingStream(output),
                   true))
        {
            var main = package.MainDocumentPart
                ?? throw new InvalidDataException(
                    "The style reference has no main document part.");
            main.Document ??= new Document(new Body());
            main.Document.Body ??= new Body();
            var body = main.Document.Body;
            var section = body.Elements<SectionProperties>()
                .LastOrDefault()?
                .CloneNode(deep: true);
            body.RemoveAllChildren();
            if (request.IncludeToc)
            {
                AppendToc(body);
            }

            var renderer = new OpenXmlMarkdownRenderer(main);
            foreach (var element in renderer.Render(content))
            {
                body.AppendChild(element);
            }

            if (section is not null)
            {
                body.AppendChild(section);
            }
            else
            {
                body.AppendChild(DefaultDocumentStyles.CreateSectionProperties());
            }

            main.Document.Save();
        }

        var collector = new Core.Diagnostics.DiagnosticCollector();
        foreach (var diagnostic in content.Diagnostics)
        {
            collector.Add(diagnostic);
        }

        output.Position = 0;
        new UpdateFieldsPostProcessor().Apply(
            output,
            new PostProcessOptions
            {
                UpdateFieldsOnOpen = request.IncludeToc,
            },
            collector);
        ValidationReport? validation = null;
        if (request.ValidateOutput)
        {
            output.Position = 0;
            validation = new OpenXmlDocumentValidator().Validate(output);
            foreach (var diagnostic in validation.Diagnostics)
            {
                collector.Add(diagnostic);
            }
        }

        output.Position = 0;
        return new ConvertResult(
            output,
            content.Stats,
            validation,
            collector.Items);
    }

    private static MemoryStream CreateBaseDocument()
    {
        var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(
                   new NonClosingStream(stream),
            WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var main = package.AddMainDocumentPart();
            main.Document = new Document(
                new Body(DefaultDocumentStyles.CreateSectionProperties()));
            var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = DefaultDocumentStyles.Create();
            stylesPart.Styles.Save();
            main.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CopyStyleReferenceAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var copy = new MemoryStream();
        await source.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
        copy.Position = 0;
        return copy;
    }

    private static async Task<byte[]> ReadAllAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static string ResolveAssetsRoot(string sourceName)
    {
        if (sourceName == "-")
        {
            return Directory.GetCurrentDirectory();
        }

        var fullPath = Path.GetFullPath(sourceName);
        return Path.GetDirectoryName(fullPath)
            ?? Directory.GetCurrentDirectory();
    }

    private static void AppendToc(Body body)
    {
        body.AppendChild(
            new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = "Heading1" }),
                new Run(new Text("Contents"))));
        body.AppendChild(
            new Paragraph(
                new Run(new FieldChar
                {
                    FieldCharType = FieldCharValues.Begin,
                    Dirty = true,
                }),
                new Run(new FieldCode(" TOC \\o \"1-3\" \\h \\z \\u ")
                {
                    Space = SpaceProcessingModeValues.Preserve,
                }),
                new Run(new FieldChar
                {
                    FieldCharType = FieldCharValues.Separate,
                }),
                new Run(new Text(
                    "Open in Microsoft Word and update fields to populate the table of contents.")),
                new Run(new FieldChar
                {
                    FieldCharType = FieldCharValues.End,
                })));
        body.AppendChild(
            new Paragraph(new Run(new Break { Type = BreakValues.Page })));
    }
}

internal static class DefaultDocumentStyles
{
    public static Styles Create()
    {
        var styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts
                        {
                            Ascii = "Aptos",
                            HighAnsi = "Aptos",
                            EastAsia = "Arial",
                            ComplexScript = "Arial",
                        },
                        new FontSize { Val = "22" },
                        new FontSizeComplexScript { Val = "22" })),
                new ParagraphPropertiesDefault(
                    new ParagraphPropertiesBaseStyle(
                        new SpacingBetweenLines
                        {
                            After = "120",
                            Line = "276",
                            LineRule = LineSpacingRuleValues.Auto,
                        }))),
            ParagraphStyle("Normal", "Normal", size: "22", after: "120"));

        var headingSizes = new[] { "36", "32", "28", "24", "22", "20" };
        for (var index = 0; index < headingSizes.Length; index++)
        {
            styles.AppendChild(
                HeadingStyle(
                    index + 1,
                    headingSizes[index],
                    before: index == 0 ? "360" : "240",
                    after: "120"));
        }

        styles.Append(
            ParagraphStyle(
                "ListParagraph",
                "List Paragraph",
                size: "22",
                after: "60"),
            ParagraphStyle(
                "Quote",
                "Quote",
                size: "21",
                after: "120",
                italic: true,
                color: "475569"),
            ParagraphStyle(
                "Code",
                "Code",
                size: "18",
                after: "120",
                font: "Cascadia Mono"),
            ParagraphStyle(
                "Caption",
                "Caption",
                size: "18",
                after: "180",
                italic: true,
                color: "64748B"),
            CharacterStyle(
                "CodeInline",
                "Code Inline",
                "Cascadia Mono",
                "20",
                "1F2937"),
            CharacterStyle(
                "Hyperlink",
                "Hyperlink",
                "Aptos",
                "22",
                "0563C1",
                underline: true),
            TableStyle());
        return styles;
    }

    public static SectionProperties CreateSectionProperties() =>
        new(
            new PageSize
            {
                Width = 12_240U,
                Height = 15_840U,
                Orient = PageOrientationValues.Portrait,
            },
            new PageMargin
            {
                Top = 1_440,
                Right = 1_440U,
                Bottom = 1_440,
                Left = 1_440U,
                Header = 720U,
                Footer = 720U,
                Gutter = 0U,
            });

    private static Style HeadingStyle(
        int level,
        string size,
        string before,
        string after) =>
        new(
            new StyleName { Val = $"Heading {level}" },
            new BasedOn { Val = "Normal" },
            new NextParagraphStyle { Val = "Normal" },
            new PrimaryStyle(),
            new StyleParagraphProperties(
                new KeepNext(),
                new KeepLines(),
                new SpacingBetweenLines
                {
                    Before = before,
                    After = after,
                },
                new OutlineLevel { Val = level - 1 }),
            new StyleRunProperties(
                new Bold(),
                new Color { Val = "16324F" },
                new FontSize { Val = size }))
        {
            Type = StyleValues.Paragraph,
            StyleId = $"Heading{level}",
        };

    private static Style ParagraphStyle(
        string id,
        string name,
        string size,
        string after,
        bool italic = false,
        string? color = null,
        string font = "Aptos")
    {
        var runProperties = new StyleRunProperties(
            new RunFonts
            {
                Ascii = font,
                HighAnsi = font,
            });
        if (italic)
        {
            runProperties.AppendChild(new Italic());
        }

        if (color is not null)
        {
            runProperties.AppendChild(new Color { Val = color });
        }

        runProperties.AppendChild(new FontSize { Val = size });

        return new Style(
            new StyleName { Val = name },
            new StyleParagraphProperties(
                new SpacingBetweenLines { After = after }),
            runProperties)
        {
            Type = StyleValues.Paragraph,
            StyleId = id,
            Default = id == "Normal",
        };
    }

    private static Style CharacterStyle(
        string id,
        string name,
        string font,
        string size,
        string color,
        bool underline = false)
    {
        var properties = new StyleRunProperties(
            new RunFonts
            {
                Ascii = font,
                HighAnsi = font,
            },
            new Color { Val = color });
        properties.AppendChild(new FontSize { Val = size });
        if (underline)
        {
            properties.AppendChild(
                new Underline { Val = UnderlineValues.Single });
        }

        return new Style(
            new StyleName { Val = name },
            properties)
        {
            Type = StyleValues.Character,
            StyleId = id,
        };
    }

    private static Style TableStyle() =>
        new(
            new StyleName { Val = "DocxGen Table" },
            new TableStyleProperties(
                new RunProperties(new Bold()),
                new TableCellProperties(
                    new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Fill = "E9EFF5",
                    }))
            {
                Type = TableStyleOverrideValues.FirstRow,
            })
        {
            Type = StyleValues.Table,
            StyleId = "DocxGenTable",
        };
}
