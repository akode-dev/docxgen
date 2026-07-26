using System.Globalization;
using Akode.DocxGen.Core.Markdown;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Akode.DocxGen.Docx.Rendering;

internal sealed class OpenXmlMarkdownRenderer
{
    private const long EmuPerPixel = 9_525L;
    private const long EmuPerTwip = 635L;
    private readonly MainDocumentPart mainPart;
    private readonly long contentWidthEmu;
    private readonly int bulletNumberId;
    private readonly int orderedNumberId;
    private readonly int uncheckedTaskNumberId;
    private readonly int checkedTaskNumberId;
    private uint nextDrawingId;

    public OpenXmlMarkdownRenderer(MainDocumentPart mainPart)
    {
        this.mainPart = mainPart ?? throw new ArgumentNullException(nameof(mainPart));
        contentWidthEmu = GetContentWidthEmu(mainPart);
        (
            bulletNumberId,
            orderedNumberId,
            uncheckedTaskNumberId,
            checkedTaskNumberId) = EnsureNumbering(mainPart);
        nextDrawingId = (mainPart.Document?.Descendants<DW.DocProperties>()
                ?? [])
            .Select(properties => properties.Id?.Value ?? 0U)
            .DefaultIfEmpty()
            .Max() + 1U;
    }

    public IReadOnlyList<OpenXmlElement> Render(MarkdownContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return RenderBlocks(content.Blocks, listLevel: 0);
    }

    private List<OpenXmlElement> RenderBlocks(
        IReadOnlyList<MarkdownBlockNode> blocks,
        int listLevel)
    {
        var result = new List<OpenXmlElement>();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case MarkdownParagraphNode paragraph:
                    result.Add(CreateParagraph(paragraph.Inlines));
                    if (paragraph.Inlines is [MarkdownImageNode { Title: { Length: > 0 } } image])
                    {
                        result.Add(CreateCaption(image.Title));
                    }

                    break;

                case MarkdownHeadingNode heading:
                    result.Add(CreateParagraph(
                        heading.Inlines,
                        ResolveStyleId($"Heading{heading.Level}", $"Heading {heading.Level}"),
                        keepWithNext: true));
                    break;

                case MarkdownCodeBlockNode code:
                    result.Add(CreateCodeBlock(code));
                    break;

                case MarkdownQuoteNode quote:
                    foreach (var element in RenderBlocks(quote.Blocks, listLevel))
                    {
                        if (element is Paragraph quotedParagraph)
                        {
                            SetStyle(quotedParagraph, ResolveStyleId("Quote"));
                        }

                        result.Add(element);
                    }

                    break;

                case MarkdownListNode list:
                    result.AddRange(RenderList(list, listLevel));
                    break;

                case MarkdownTableNode table:
                    result.Add(CreateTable(table));
                    break;

                case MarkdownHorizontalRuleNode:
                    result.Add(CreateHorizontalRule());
                    break;
            }
        }

        return result;
    }

    private List<OpenXmlElement> RenderList(MarkdownListNode list, int level)
    {
        var result = new List<OpenXmlElement>();
        var numberingId = list.Ordered
            ? CreateRestartedNumbering(
                orderedNumberId,
                Math.Clamp(level, 0, 8),
                list.Start)
            : bulletNumberId;
        foreach (var item in list.Items)
        {
            var numbered = false;
            foreach (var block in item.Blocks)
            {
                if (!numbered && block is MarkdownParagraphNode paragraph)
                {
                    var itemNumberingId = numberingId;
                    IReadOnlyList<MarkdownInlineNode> paragraphInlines =
                        paragraph.Inlines;
                    if (paragraph.Inlines.Count > 0
                        && paragraph.Inlines[0] is MarkdownTaskListNode taskList)
                    {
                        itemNumberingId = taskList.Checked
                            ? checkedTaskNumberId
                            : uncheckedTaskNumberId;
                        paragraphInlines = RemoveTaskListMarker(
                            paragraph.Inlines);
                    }

                    result.Add(CreateParagraph(
                        paragraphInlines,
                        ResolveStyleId("ListParagraph", "List Paragraph"),
                        itemNumberingId,
                        Math.Clamp(level, 0, 8)));
                    numbered = true;
                    continue;
                }

                if (block is MarkdownListNode nested)
                {
                    result.AddRange(RenderList(nested, level + 1));
                    continue;
                }

                if (!numbered)
                {
                    result.Add(CreateParagraph(
                        [],
                        ResolveStyleId("ListParagraph", "List Paragraph"),
                        numberingId,
                        Math.Clamp(level, 0, 8)));
                    numbered = true;
                }

                result.AddRange(RenderBlocks([block], level));
            }

            if (!numbered)
            {
                result.Add(CreateParagraph(
                    [],
                    ResolveStyleId("ListParagraph", "List Paragraph"),
                    numberingId,
                    Math.Clamp(level, 0, 8)));
            }
        }

        return result;
    }

    private static MarkdownInlineNode[] RemoveTaskListMarker(
        IReadOnlyList<MarkdownInlineNode> inlines)
    {
        var content = new MarkdownInlineNode[inlines.Count - 1];
        for (var index = 1; index < inlines.Count; index++)
        {
            content[index - 1] = inlines[index];
        }

        if (content.Length > 0 && content[0] is MarkdownTextNode text)
        {
            content[0] = new MarkdownTextNode(text.Text.TrimStart());
        }

        return content;
    }

    private Paragraph CreateParagraph(
        IReadOnlyList<MarkdownInlineNode> inlines,
        string? styleId = null,
        int? numberingId = null,
        int numberingLevel = 0,
        bool keepWithNext = false)
    {
        var properties = new ParagraphProperties();
        if (!string.IsNullOrWhiteSpace(styleId))
        {
            properties.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
        }

        if (numberingId is not null)
        {
            properties.NumberingProperties = new NumberingProperties(
                new NumberingLevelReference { Val = numberingLevel },
                new NumberingId { Val = numberingId.Value });
        }

        if (keepWithNext)
        {
            properties.KeepNext = new KeepNext();
        }

        var paragraph = new Paragraph(properties);
        if (inlines.Count == 1 && inlines[0] is MarkdownImageNode)
        {
            properties.Justification = new Justification
            {
                Val = JustificationValues.Center,
            };
        }

        AppendInlines(paragraph, inlines, InlineFormatting.None);
        if (!paragraph.ChildElements.Any(element => element is Run or Hyperlink))
        {
            paragraph.AppendChild(new Run(new Text(string.Empty)));
        }

        return paragraph;
    }

    private Paragraph CreateCodeBlock(MarkdownCodeBlockNode code)
    {
        var styleId = ResolveStyleId("Code");
        var paragraph = CreateParagraph([], styleId);
        var properties = paragraph.ParagraphProperties!;
        if (styleId is null)
        {
            properties.Shading = new Shading
            {
                Val = ShadingPatternValues.Clear,
                Fill = "F3F4F6",
            };
            properties.SpacingBetweenLines = new SpacingBetweenLines
            {
                Before = "120",
                After = "120",
                Line = "276",
                LineRule = LineSpacingRuleValues.Auto,
            };
        }

        paragraph.RemoveAllChildren<Run>();
        var lines = code.Code.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var run = new Run(
                new RunProperties(
                    new RunFonts
                    {
                        Ascii = "Cascadia Mono",
                        HighAnsi = "Cascadia Mono",
                    },
                    new FontSize { Val = "18" }),
                new Text(lines[index]) { Space = SpaceProcessingModeValues.Preserve });
            if (index > 0)
            {
                run.InsertAt(new Break(), 1);
            }

            paragraph.AppendChild(run);
        }

        return paragraph;
    }

    private Paragraph CreateCaption(string text) =>
        CreateParagraph(
            [new MarkdownTextNode(text)],
            ResolveStyleId("Caption"));

    private Paragraph CreateHorizontalRule()
    {
        var paragraph = CreateParagraph([]);
        paragraph.ParagraphProperties!.ParagraphBorders = new ParagraphBorders(
            new BottomBorder
            {
                Val = BorderValues.Single,
                Color = "B8C2CC",
                Size = 4U,
                Space = 4U,
            });
        return paragraph;
    }

    private Table CreateTable(MarkdownTableNode source)
    {
        var columnCount = Math.Max(
            source.Header.Count,
            source.Rows.Select(row => row.Count).DefaultIfEmpty().Max());
        columnCount = Math.Max(columnCount, 1);
        var widths = CalculateColumnWidths(source, columnCount);
        var table = new Table(
            new TableProperties(
                new TableStyle
                {
                    Val = ResolveStyleId("DocxGenTable", "TableGrid", "Table Grid")
                        ?? "TableGrid",
                },
                new TableWidth
                {
                    Type = TableWidthUnitValues.Dxa,
                    Width = widths.Sum().ToString(CultureInfo.InvariantCulture),
                },
                new TableLayout { Type = TableLayoutValues.Fixed },
                new TableCellMarginDefault(
                    new TopMargin
                    {
                        Width = "80",
                        Type = TableWidthUnitValues.Dxa,
                    },
                    new TableCellLeftMargin
                    {
                        Width = 120,
                        Type = TableWidthValues.Dxa,
                    },
                    new BottomMargin
                    {
                        Width = "80",
                        Type = TableWidthUnitValues.Dxa,
                    },
                    new TableCellRightMargin
                    {
                        Width = 120,
                        Type = TableWidthValues.Dxa,
                    })),
            new TableGrid(widths.Select(width => new GridColumn
            {
                Width = width.ToString(CultureInfo.InvariantCulture),
            })));

        if (source.Header.Count > 0)
        {
            var header = CreateTableRow(source.Header, widths, isHeader: true);
            header.TableRowProperties ??= new TableRowProperties();
            header.TableRowProperties.AppendChild(new TableHeader());
            table.AppendChild(header);
        }

        foreach (var row in source.Rows)
        {
            table.AppendChild(CreateTableRow(row, widths, isHeader: false));
        }

        return table;
    }

    private TableRow CreateTableRow(
        IReadOnlyList<IReadOnlyList<MarkdownInlineNode>> values,
        int[] widths,
        bool isHeader)
    {
        var row = new TableRow();
        for (var index = 0; index < widths.Length; index++)
        {
            var properties = new TableCellProperties(
                new TableCellWidth
                {
                    Type = TableWidthUnitValues.Dxa,
                    Width = widths[index].ToString(CultureInfo.InvariantCulture),
                },
                new TableCellVerticalAlignment
                {
                    Val = TableVerticalAlignmentValues.Center,
                });
            if (isHeader)
            {
                properties.Shading = new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Fill = "E9EFF5",
                };
            }

            var inlines = index < values.Count ? values[index] : [];
            var paragraph = CreateParagraph(inlines);
            paragraph.ParagraphProperties!.SpacingBetweenLines =
                new SpacingBetweenLines
                {
                    Before = "0",
                    After = "0",
                };
            if (isHeader)
            {
                foreach (var run in paragraph.Descendants<Run>())
                {
                    run.RunProperties ??= new RunProperties();
                    run.RunProperties.Bold = new Bold();
                }
            }

            row.AppendChild(new TableCell(properties, paragraph));
        }

        return row;
    }

    private int[] CalculateColumnWidths(MarkdownTableNode table, int columnCount)
    {
        var weights = Enumerable.Repeat(8, columnCount).ToArray();
        ApplyWeights(table.Header, weights);
        foreach (var row in table.Rows)
        {
            ApplyWeights(row, weights);
        }

        const int minimumWidth = 900;
        var availableTwips = checked((int)(contentWidthEmu / EmuPerTwip));
        var remaining = availableTwips - (minimumWidth * columnCount);
        if (remaining <= 0)
        {
            return Enumerable.Repeat(
                Math.Max(availableTwips / columnCount, 1),
                columnCount).ToArray();
        }

        var totalWeight = weights.Sum();
        var widths = weights
            .Select(weight => minimumWidth + (remaining * weight / totalWeight))
            .ToArray();
        widths[^1] += availableTwips - widths.Sum();
        return widths;
    }

    private static void ApplyWeights(
        IReadOnlyList<IReadOnlyList<MarkdownInlineNode>> row,
        int[] weights)
    {
        for (var index = 0; index < row.Count && index < weights.Length; index++)
        {
            weights[index] = Math.Max(
                weights[index],
                Math.Clamp(PlainTextLength(row[index]), 8, 40));
        }
    }

    private static int PlainTextLength(IReadOnlyList<MarkdownInlineNode> inlines) =>
        inlines.Sum(PlainTextLength);

    private static int PlainTextLength(MarkdownInlineNode inline) =>
        inline switch
        {
            MarkdownTextNode text => text.Text.Length,
            MarkdownCodeInlineNode code => code.Code.Length,
            MarkdownTaskListNode => 2,
            MarkdownEmphasisNode emphasis => PlainTextLength(emphasis.Children),
            MarkdownLinkNode link => PlainTextLength(link.Children),
            MarkdownImageNode image => image.AlternativeText.Length,
            _ => 1,
        };

    private void AppendInlines(
        OpenXmlCompositeElement parent,
        IReadOnlyList<MarkdownInlineNode> inlines,
        InlineFormatting formatting)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case MarkdownTextNode text:
                    parent.AppendChild(CreateTextRun(text.Text, formatting));
                    break;

                case MarkdownBreakNode { Hard: true }:
                    parent.AppendChild(new Run(new Break()));
                    break;

                case MarkdownBreakNode:
                    parent.AppendChild(CreateTextRun(" ", formatting));
                    break;

                case MarkdownCodeInlineNode code:
                    parent.AppendChild(CreateTextRun(
                        code.Code,
                        formatting with { Code = true }));
                    break;

                case MarkdownTaskListNode taskList:
                    parent.AppendChild(CreateTextRun(
                        taskList.Checked ? "☒ " : "☐ ",
                        formatting));
                    break;

                case MarkdownEmphasisNode emphasis:
                    AppendInlines(
                        parent,
                        emphasis.Children,
                        formatting with
                        {
                            Bold = formatting.Bold || emphasis.Bold,
                            Italic = formatting.Italic || emphasis.Italic,
                            Strikethrough =
                                formatting.Strikethrough || emphasis.Strikethrough,
                        });
                    break;

                case MarkdownLinkNode link:
                    parent.AppendChild(CreateHyperlink(link, formatting));
                    break;

                case MarkdownImageNode image:
                    parent.AppendChild(new Run(CreateImageDrawing(image)));
                    break;
            }
        }
    }

    private Run CreateTextRun(string text, InlineFormatting formatting)
    {
        var properties = new RunProperties();
        if (formatting.Bold)
        {
            properties.Bold = new Bold();
        }

        if (formatting.Italic)
        {
            properties.Italic = new Italic();
        }

        if (formatting.Strikethrough)
        {
            properties.Strike = new Strike();
        }

        if (formatting.Code)
        {
            var codeStyle = ResolveStyleId("CodeInline");
            if (codeStyle is not null)
            {
                properties.RunStyle = new RunStyle { Val = codeStyle };
            }
            else
            {
                properties.RunFonts = new RunFonts
                {
                    Ascii = "Cascadia Mono",
                    HighAnsi = "Cascadia Mono",
                };
                properties.Shading = new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Fill = "F3F4F6",
                };
            }
        }

        return new Run(
            properties,
            new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private Hyperlink CreateHyperlink(
        MarkdownLinkNode link,
        InlineFormatting formatting)
    {
        var hyperlink = new Hyperlink { History = true };
        var value = link.Url.OriginalString;
        if (value.StartsWith('#'))
        {
            hyperlink.Anchor = value[1..];
        }
        else
        {
            var relationship = mainPart.AddHyperlinkRelationship(link.Url, true);
            hyperlink.Id = relationship.Id;
        }

        var hyperlinkStyle = ResolveStyleId("Hyperlink");
        foreach (var child in link.Children)
        {
            var staging = new Paragraph();
            AppendInlines(staging, [child], formatting);
            foreach (var element in staging.ChildElements.ToArray())
            {
                element.Remove();
                if (element is Run run && hyperlinkStyle is not null)
                {
                    run.RunProperties ??= new RunProperties();
                    run.RunProperties.RunStyle = new RunStyle { Val = hyperlinkStyle };
                }

                hyperlink.AppendChild(element);
            }
        }

        return hyperlink;
    }

    private W.Drawing CreateImageDrawing(MarkdownImageNode image)
    {
        var imagePart = image.Asset.MediaType switch
        {
            "image/png" => mainPart.AddImagePart(ImagePartType.Png),
            "image/jpeg" => mainPart.AddImagePart(ImagePartType.Jpeg),
            "image/gif" => mainPart.AddImagePart(ImagePartType.Gif),
            "image/bmp" => mainPart.AddImagePart(ImagePartType.Bmp),
            "image/svg+xml" => mainPart.AddImagePart(ImagePartType.Svg),
            _ => throw new InvalidDataException(
                $"Unsupported image media type '{image.Asset.MediaType}'."),
        };
        using (var stream = new MemoryStream(
                   image.Asset.Content.ToArray(),
                   writable: false))
        {
            imagePart.FeedData(stream);
        }

        var (pixelWidth, pixelHeight) = ImageDimensions.Read(
            image.Asset.Content.Span,
            image.Asset.MediaType);
        var width = Math.Max(pixelWidth, 1) * EmuPerPixel;
        var height = Math.Max(pixelHeight, 1) * EmuPerPixel;
        if (width > contentWidthEmu)
        {
            height = checked(height * contentWidthEmu / width);
            width = contentWidthEmu;
        }

        var id = nextDrawingId++;
        var relationshipId = mainPart.GetIdOfPart(imagePart);
        return new W.Drawing(
            new DW.Inline(
                new DW.Extent { Cx = width, Cy = height },
                new DW.EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L,
                },
                new DW.DocProperties
                {
                    Id = id,
                    Name = Path.GetFileName(image.Asset.FullPath),
                    Description = string.IsNullOrWhiteSpace(image.AlternativeText)
                        ? Path.GetFileNameWithoutExtension(image.Asset.FullPath)
                        : image.AlternativeText,
                },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties
                                {
                                    Id = id,
                                    Name = Path.GetFileName(image.Asset.FullPath),
                                },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip
                                {
                                    Embed = relationshipId,
                                    CompressionState =
                                        A.BlipCompressionValues.Print,
                                },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = width, Cy = height }),
                                new A.PresetGeometry
                                {
                                    Preset = A.ShapeTypeValues.Rectangle,
                                })))
                    {
                        Uri =
                            "http://schemas.openxmlformats.org/drawingml/2006/picture",
                    }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
            });
    }

    private string? ResolveStyleId(params string[] candidates)
    {
        var styles = mainPart.StyleDefinitionsPart?.Styles;
        if (styles is null)
        {
            return candidates.FirstOrDefault();
        }

        foreach (var candidate in candidates)
        {
            var match = styles.Elements<Style>().FirstOrDefault(style =>
                string.Equals(
                    style.StyleId?.Value,
                    candidate,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    style.StyleName?.Val?.Value,
                    candidate,
                    StringComparison.OrdinalIgnoreCase));
            if (match?.StyleId?.Value is { Length: > 0 } id)
            {
                return id;
            }
        }

        return null;
    }

    private static void SetStyle(Paragraph paragraph, string? styleId)
    {
        if (styleId is null)
        {
            return;
        }

        paragraph.ParagraphProperties ??= new ParagraphProperties();
        paragraph.ParagraphProperties.ParagraphStyleId =
            new ParagraphStyleId { Val = styleId };
    }

    private static long GetContentWidthEmu(MainDocumentPart main)
    {
        var section = main.Document?.Body?
            .Descendants<SectionProperties>()
            .LastOrDefault();
        var pageWidth = section?.GetFirstChild<PageSize>()?.Width?.Value ?? 12_240U;
        var margins = section?.GetFirstChild<PageMargin>();
        var left = margins?.Left?.Value ?? 1_440;
        var right = margins?.Right?.Value ?? 1_440;
        return Math.Max((long)pageWidth - left - right, 1L) * EmuPerTwip;
    }

    private static (
        int Bullet,
        int Ordered,
        int UncheckedTask,
        int CheckedTask) EnsureNumbering(MainDocumentPart main)
    {
        var part = main.NumberingDefinitionsPart
            ?? main.AddNewPart<NumberingDefinitionsPart>();
        part.Numbering ??= new Numbering();
        var nextAbstractId = part.Numbering
            .Elements<AbstractNum>()
            .Select(item => item.AbstractNumberId?.Value ?? -1)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        var nextNumberId = part.Numbering
            .Elements<NumberingInstance>()
            .Select(item => item.NumberID?.Value ?? 0)
            .DefaultIfEmpty()
            .Max() + 1;

        var bulletAbstract = CreateAbstractNumber(nextAbstractId, ordered: false);
        var orderedAbstract = CreateAbstractNumber(nextAbstractId + 1, ordered: true);
        var uncheckedTaskAbstract = CreateAbstractNumber(
            nextAbstractId + 2,
            ordered: false,
            taskMarker: "☐");
        var checkedTaskAbstract = CreateAbstractNumber(
            nextAbstractId + 3,
            ordered: false,
            taskMarker: "☒");
        InsertAbstractNumber(part.Numbering, bulletAbstract);
        InsertAbstractNumber(part.Numbering, orderedAbstract);
        InsertAbstractNumber(part.Numbering, uncheckedTaskAbstract);
        InsertAbstractNumber(part.Numbering, checkedTaskAbstract);
        var bulletInstance = new NumberingInstance(
            new AbstractNumId { Val = nextAbstractId })
        {
            NumberID = nextNumberId,
        };
        var orderedInstance = new NumberingInstance(
            new AbstractNumId { Val = nextAbstractId + 1 })
        {
            NumberID = nextNumberId + 1,
        };
        var uncheckedTaskInstance = new NumberingInstance(
            new AbstractNumId { Val = nextAbstractId + 2 })
        {
            NumberID = nextNumberId + 2,
        };
        var checkedTaskInstance = new NumberingInstance(
            new AbstractNumId { Val = nextAbstractId + 3 })
        {
            NumberID = nextNumberId + 3,
        };
        InsertNumberingInstance(part.Numbering, bulletInstance);
        InsertNumberingInstance(part.Numbering, orderedInstance);
        InsertNumberingInstance(part.Numbering, uncheckedTaskInstance);
        InsertNumberingInstance(part.Numbering, checkedTaskInstance);
        part.Numbering.Save();
        return (
            nextNumberId,
            nextNumberId + 1,
            nextNumberId + 2,
            nextNumberId + 3);
    }

    private static void InsertAbstractNumber(
        Numbering numbering,
        AbstractNum abstractNumber)
    {
        var anchor = numbering.ChildElements.FirstOrDefault(
            element => element is NumberingInstance or NumberingIdMacAtCleanup);
        if (anchor is null)
        {
            numbering.AppendChild(abstractNumber);
        }
        else
        {
            numbering.InsertBefore(abstractNumber, anchor);
        }
    }

    private static void InsertNumberingInstance(
        Numbering numbering,
        NumberingInstance instance)
    {
        var cleanup = numbering.GetFirstChild<NumberingIdMacAtCleanup>();
        if (cleanup is null)
        {
            numbering.AppendChild(instance);
        }
        else
        {
            numbering.InsertBefore(instance, cleanup);
        }
    }

    private static AbstractNum CreateAbstractNumber(
        int id,
        bool ordered,
        string? taskMarker = null)
    {
        var abstractNumber = new AbstractNum { AbstractNumberId = id };
        var bullets = new[] { "•", "○", "▪" };
        for (var level = 0; level < 9; level++)
        {
            abstractNumber.AppendChild(
                new Level(
                    new StartNumberingValue { Val = 1 },
                    new NumberingFormat
                    {
                        Val = ordered
                            ? NumberFormatValues.Decimal
                            : NumberFormatValues.Bullet,
                    },
                    new LevelText
                    {
                        Val = ordered
                            ? $"%{level + 1}."
                            : taskMarker ?? bullets[level % bullets.Length],
                    },
                    new LevelJustification { Val = LevelJustificationValues.Left },
                    new PreviousParagraphProperties(
                        new Indentation
                        {
                            Left = ((level + 1) * 720)
                                .ToString(CultureInfo.InvariantCulture),
                            Hanging = "360",
                        }))
                {
                    LevelIndex = level,
                });
        }

        return abstractNumber;
    }

    private int CreateRestartedNumbering(
        int baseNumberId,
        int level,
        int start)
    {
        var part = mainPart.NumberingDefinitionsPart
            ?? throw new InvalidDataException(
                "The document numbering part is missing.");
        var numbering = part.Numbering
            ?? throw new InvalidDataException(
                "The document numbering root is missing.");
        var baseInstance = numbering.Elements<NumberingInstance>()
            .Single(instance => instance.NumberID?.Value == baseNumberId);
        var abstractId = baseInstance.AbstractNumId?.Val?.Value
            ?? throw new InvalidDataException(
                "The base numbering instance has no abstract numbering id.");
        var numberId = numbering.Elements<NumberingInstance>()
            .Select(instance => instance.NumberID?.Value ?? 0)
            .DefaultIfEmpty()
            .Max() + 1;
        var instance = new NumberingInstance(
            new AbstractNumId { Val = abstractId })
        {
            NumberID = numberId,
        };
        instance.AppendChild(
            new LevelOverride(
                new StartOverrideNumberingValue { Val = start })
            {
                LevelIndex = level,
            });

        InsertNumberingInstance(numbering, instance);
        numbering.Save();
        return numberId;
    }

    private readonly record struct InlineFormatting(
        bool Bold,
        bool Italic,
        bool Strikethrough,
        bool Code)
    {
        public static InlineFormatting None { get; } =
            new(Bold: false, Italic: false, Strikethrough: false, Code: false);
    }
}

internal static class ImageDimensions
{
    public static (long Width, long Height) Read(
        ReadOnlySpan<byte> bytes,
        string mediaType) =>
        mediaType switch
        {
            "image/png" => ReadPng(bytes),
            "image/jpeg" => ReadJpeg(bytes),
            "image/gif" => ReadGif(bytes),
            "image/bmp" => ReadBmp(bytes),
            _ => (1200L, 675L),
        };

    private static (long Width, long Height) ReadPng(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 24
            || !bytes[..8].SequenceEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            return (1200L, 675L);
        }

        return (
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes[16..20]),
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes[20..24]));
    }

    private static (long Width, long Height) ReadGif(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 10)
        {
            return (1200L, 675L);
        }

        return (
            System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..8]),
            System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..10]));
    }

    private static (long Width, long Height) ReadBmp(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 26 || bytes[0] != 'B' || bytes[1] != 'M')
        {
            return (1200L, 675L);
        }

        return (
            Math.Abs(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                bytes[18..22])),
            Math.Abs(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                bytes[22..26])));
    }

    private static (long Width, long Height) ReadJpeg(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return (1200L, 675L);
        }

        var offset = 2;
        while (offset + 8 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = bytes[offset + 1];
            if (marker is >= 0xC0 and <= 0xC3
                or >= 0xC5 and <= 0xC7
                or >= 0xC9 and <= 0xCB
                or >= 0xCD and <= 0xCF)
            {
                return (
                    System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(
                        bytes[(offset + 7)..(offset + 9)]),
                    System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(
                        bytes[(offset + 5)..(offset + 7)]));
            }

            if (marker is 0xD8 or 0xD9)
            {
                offset += 2;
                continue;
            }

            var length = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(
                bytes[(offset + 2)..(offset + 4)]);
            if (length < 2)
            {
                break;
            }

            offset += 2 + length;
        }

        return (1200L, 675L);
    }
}
