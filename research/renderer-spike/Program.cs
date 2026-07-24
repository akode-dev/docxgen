using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplater;
using DocxTemplater.Formatter;
using DocxTemplater.Markdown;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;

if (args is ["--describe-api"])
{
    DescribeApi();
    return 0;
}

if (args.Length != 5)
{
    Console.Error.WriteLine(
        "Usage: renderer-spike <template.docx> <model.json> <body.md> <figure.svg> <output.docx>");
    return 64;
}

var templatePath = Path.GetFullPath(args[0]);
var modelPath = Path.GetFullPath(args[1]);
var markdownPath = Path.GetFullPath(args[2]);
var figurePath = Path.GetFullPath(args[3]);
var outputPath = Path.GetFullPath(args[4]);

foreach (var path in new[] { templatePath, modelPath, markdownPath, figurePath })
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"Input does not exist: {path}");
        return 66;
    }
}

var modelJson = await File.ReadAllTextAsync(modelPath);
var sourceModel = JsonSerializer.Deserialize(
    modelJson,
    SpikeJsonContext.Default.SpikeSourceModel)
    ?? throw new InvalidDataException("model.json is empty.");
var markdown = await File.ReadAllTextAsync(markdownPath);
var model = new SpikeTemplateModel(
    sourceModel.Document,
    sourceModel.Revisions,
    markdown);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using (var template = DocxTemplate.Open(templatePath))
{
    template.RegisterFormatter(CreateMarkdownFormatter());

    var schema = template.GetTemplateSchema();
    var schemaPath = Path.ChangeExtension(outputPath, ".template-schema.json");
    await File.WriteAllTextAsync(
        schemaPath,
        JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        }));

    template.BindModel("ds", model);
    template.Save(outputPath);
}

using (var document = WordprocessingDocument.Open(outputPath, true))
{
    InsertFigure(document, figurePath);
    EnableFieldUpdates(document);

    var validator = new OpenXmlValidator(FileFormatVersions.Microsoft365);
    var errors = validator.Validate(document).ToArray();
    if (errors.Length > 0)
    {
        foreach (var error in errors.Take(20))
        {
            Console.Error.WriteLine(
                $"OpenXml: {error.Part?.Uri} {error.Path?.XPath}: {error.Description}");
        }

        return 2;
    }
}

Console.WriteLine(outputPath);
return 0;

static MarkdownFormatter CreateMarkdownFormatter()
{
    var configuration = new MarkDownFormatterConfiguration
    {
        TableStyle = "AkodeTable",
        OrderedListStyle = "List Number",
        UnorderedListStyle = "List Bullet",
    };

    return new MarkdownFormatter(configuration);
}

static void DescribeApi()
{
    var formatterType = typeof(MarkdownFormatter);
    var configurationType = typeof(MarkDownFormatterConfiguration);

    Console.WriteLine(formatterType.FullName);
    foreach (var constructor in formatterType.GetConstructors())
    {
        Console.WriteLine($"  ctor {constructor}");
    }

    Console.WriteLine(configurationType.FullName);
    foreach (var constructor in configurationType.GetConstructors())
    {
        Console.WriteLine($"  ctor {constructor}");
    }

    foreach (var property in configurationType.GetProperties())
    {
        Console.WriteLine(
            $"  {property.PropertyType.Name} {property.Name} set={property.SetMethod is not null}");
    }
}

static void EnableFieldUpdates(WordprocessingDocument document)
{
    var mainPart = document.MainDocumentPart
        ?? throw new InvalidDataException("The document has no main document part.");
    var settingsPart = mainPart.DocumentSettingsPart
        ?? mainPart.AddNewPart<DocumentSettingsPart>();
    settingsPart.Settings ??= new Settings();

    var updateFields = settingsPart.Settings.GetFirstChild<UpdateFieldsOnOpen>();
    if (updateFields is null)
    {
        settingsPart.Settings.AddChild(new UpdateFieldsOnOpen { Val = true }, true);
    }
    else
    {
        updateFields.Val = true;
    }

    settingsPart.Settings.Save();
}

static void InsertFigure(WordprocessingDocument document, string figurePath)
{
    const string marker = "[[DOCXGEN_IMAGE:architecture.svg]]";
    var mainPart = document.MainDocumentPart
        ?? throw new InvalidDataException("The document has no main document part.");
    var mainDocument = mainPart.Document
        ?? throw new InvalidDataException("The main document part is empty.");
    var body = mainDocument.Body
        ?? throw new InvalidDataException("The document has no body.");
    var paragraph = body
        .Descendants<Paragraph>()
        .SingleOrDefault(item => string.Equals(item.InnerText.Trim(), marker, StringComparison.Ordinal));

    if (paragraph is null)
    {
        throw new InvalidDataException($"Figure marker was not found: {marker}");
    }

    var imagePart = mainPart.AddImagePart(ImagePartType.Svg);
    using (var stream = File.OpenRead(figurePath))
    {
        imagePart.FeedData(stream);
    }

    var relationshipId = mainPart.GetIdOfPart(imagePart);
    const long widthEmu = 5_486_400L;
    const long heightEmu = 1_920_240L;
    var drawing = CreateInlineDrawing(relationshipId, widthEmu, heightEmu);

    paragraph.RemoveAllChildren<Run>();
    paragraph.ParagraphProperties ??= new ParagraphProperties();
    paragraph.ParagraphProperties.Justification = new Justification
    {
        Val = JustificationValues.Center,
    };
    paragraph.AppendChild(new Run(drawing));
}

static W.Drawing CreateInlineDrawing(string relationshipId, long widthEmu, long heightEmu)
{
    var element = new W.Drawing(
        new DW.Inline(
            new DW.Extent { Cx = widthEmu, Cy = heightEmu },
            new DW.EffectExtent
            {
                LeftEdge = 0L,
                TopEdge = 0L,
                RightEdge = 0L,
                BottomEdge = 0L,
            },
            new DW.DocProperties
            {
                Id = 1U,
                Name = "Architecture figure",
                Description = "Akode.DocxGen rendering pipeline",
            },
            new DW.NonVisualGraphicFrameDrawingProperties(
                new A.GraphicFrameLocks { NoChangeAspect = true }),
            new A.Graphic(
                new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties
                            {
                                Id = 0U,
                                Name = "architecture.svg",
                            },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(
                            new A.Blip
                            {
                                Embed = relationshipId,
                                CompressionState = A.BlipCompressionValues.Print,
                            },
                            new A.Stretch(new A.FillRectangle())),
                        new PIC.ShapeProperties(
                            new A.Transform2D(
                                new A.Offset { X = 0L, Y = 0L },
                                new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                            new A.PresetGeometry
                            {
                                Preset = A.ShapeTypeValues.Rectangle,
                            })))
                {
                    Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture",
                }))
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U,
        });

    return element;
}

internal sealed record SpikeSourceModel(
    SpikeDocument Document,
    IReadOnlyList<SpikeRevision> Revisions);

internal sealed record SpikeTemplateModel(
    SpikeDocument Document,
    IReadOnlyList<SpikeRevision> Revisions,
    string Body);

internal sealed record SpikeDocument(
    string Title,
    string Description,
    string Project,
    string Client,
    string Version,
    string Status,
    string Date,
    string Classification,
    SpikeAuthor Author);

internal sealed record SpikeAuthor(
    string FirstName,
    string LastName,
    string Role,
    string Email);

internal sealed record SpikeRevision(
    string Version,
    string Date,
    string Author,
    string Description);

[System.Text.Json.Serialization.JsonSourceGenerationOptions(
    PropertyNamingPolicy = System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
[System.Text.Json.Serialization.JsonSerializable(typeof(SpikeSourceModel))]
internal sealed partial class SpikeJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
