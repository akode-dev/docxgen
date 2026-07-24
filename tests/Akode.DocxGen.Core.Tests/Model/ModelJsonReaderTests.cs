using System.Security.Cryptography;
using System.Text;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Model;

public sealed class ModelJsonReaderTests
{
    [Fact]
    public async Task ReadsEnvelopeOptionsAndEverySupportedValueKind()
    {
        const string json =
            """
            {
              "modelVersion": "1.0",
              "template": {
                "id": "akode-proposal",
                "version": "1.0.0"
              },
              "options": {
                "culture": "pl-PL",
                "strict": false,
                "headingOffset": 2,
                "allowRawHtml": true,
                "allowRemoteImages": false,
                "updateFieldsOnOpen": false
              },
              "data": {
                "ds": {
                  "Title": "Proposal",
                  "PageCount": 12,
                  "Approved": true,
                  "Optional": null,
                  "Body": { "$md": "# Approach" },
                  "BodyFile": { "$mdFile": "sections/approach.md" },
                  "Logo": { "$file": "assets/logo.png" },
                  "Literal": { "$text": "*not Markdown*" },
                  "Items": [
                    { "Name": "First" },
                    { "Name": "Second" }
                  ]
                }
              }
            }
            """;

        var result = await ReadAsync(json).ConfigureAwait(true);

        result.IsValid.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        result.ModelHash.ShouldMatch("^sha256:[0-9a-f]{64}$");

        var model = result.Model.ShouldNotBeNull();
        model.ModelVersion.ShouldBe("1.0");
        model.Template.ShouldBe(new TemplateReference("akode-proposal", "1.0.0"));
        model.Options.ShouldBe(
            new ModelDocumentOptions(
                "pl-PL",
                Strict: false,
                HeadingOffset: 2,
                AllowRawHtml: true,
                AllowRemoteImages: false,
                UpdateFieldsOnOpen: false));

        var dataSource = model.Data["ds"].ShouldBeOfType<ObjectModelValue>();
        dataSource.Properties["Title"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetString()
            .ShouldBe("Proposal");
        dataSource.Properties["PageCount"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetInt32()
            .ShouldBe(12);
        dataSource.Properties["Approved"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.GetBoolean()
            .ShouldBeTrue();
        dataSource.Properties["Optional"]
            .ShouldBeOfType<PrimitiveModelValue>()
            .Value.ValueKind
            .ShouldBe(System.Text.Json.JsonValueKind.Null);
        dataSource.Properties["Body"]
            .ShouldBe(new InlineMarkdownModelValue("# Approach"));
        dataSource.Properties["BodyFile"]
            .ShouldBe(new MarkdownFileModelValue("sections/approach.md"));
        dataSource.Properties["Logo"]
            .ShouldBe(new FileModelValue("assets/logo.png"));
        dataSource.Properties["Literal"]
            .ShouldBe(new PlainTextModelValue("*not Markdown*"));

        var items = dataSource.Properties["Items"].ShouldBeOfType<CollectionModelValue>();
        items.Items.Count.ShouldBe(2);
        items.Items.ShouldAllBe(item => item is ObjectModelValue);
    }

    [Fact]
    public async Task AcceptsUtf8BomAndAppliesSafeDefaults()
    {
        const string json =
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "minimal", "version": "1.0.0" },
              "data": { "ds": {} }
            }
            """;
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var bytes = new byte[Encoding.UTF8.Preamble.Length + jsonBytes.Length];
        Encoding.UTF8.Preamble.CopyTo(bytes.AsSpan());
        jsonBytes.CopyTo(bytes, Encoding.UTF8.Preamble.Length);

        var result = await ReadAsync(bytes).ConfigureAwait(true);

        result.IsValid.ShouldBeTrue();
        result.Model.ShouldNotBeNull().Options.ShouldBe(ModelDocumentOptions.Default);
        result.ModelHash.ShouldBe(
            $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
    }

    [Fact]
    public async Task ReportsMalformedJsonWithoutThrowing()
    {
        var result = await ReadAsync("""{ "modelVersion": "1.0", """)
            .ConfigureAwait(true);

        result.IsValid.ShouldBeFalse();
        result.Model.ShouldBeNull();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.ModelInvalidJson);
        diagnostic.Path.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ReportsBaseSchemaViolationsWithMachineReadablePaths()
    {
        const string json =
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "minimal" },
              "data": {}
            }
            """;

        var result = await ReadAsync(json).ConfigureAwait(true);

        result.IsValid.ShouldBeFalse();
        result.Model.ShouldBeNull();
        result.Diagnostics.ShouldContain(
            diagnostic =>
                diagnostic.Code == DiagnosticCode.ModelSchemaViolation
                && !string.IsNullOrWhiteSpace(diagnostic.Path));
    }

    [Fact]
    public async Task ReportsUnknownDirectiveAtExactJsonPointer()
    {
        const string json =
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "minimal", "version": "1.0.0" },
              "data": {
                "ds": {
                  "Body": { "$html": "<p>Unsupported</p>" }
                }
              }
            }
            """;

        var result = await ReadAsync(json).ConfigureAwait(true);

        result.Model.ShouldBeNull();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.ModelUnknownDirective);
        diagnostic.Path.ShouldBe("/data/ds/Body/$html");
        diagnostic.Hint.ShouldContain("$mdFile");
    }

    [Fact]
    public async Task RejectsDuplicatePropertiesAtExactJsonPointer()
    {
        const string json =
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "minimal", "version": "1.0.0" },
              "data": {
                "ds": {
                  "Title": "First",
                  "Title": "Second"
                }
              }
            }
            """;

        var result = await ReadAsync(json).ConfigureAwait(true);

        result.Model.ShouldBeNull();
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Code.ShouldBe(DiagnosticCode.ModelInvalidJson);
        diagnostic.Path.ShouldBe("/data/ds/Title");
        diagnostic.Message.ShouldContain("duplicate property");
    }

    [Fact]
    public async Task RejectsDirectiveObjectsWithAdditionalProperties()
    {
        const string json =
            """
            {
              "modelVersion": "1.0",
              "template": { "id": "minimal", "version": "1.0.0" },
              "data": {
                "ds": {
                  "Body": {
                    "$md": "# Approach",
                    "unexpected": true
                  }
                }
              }
            }
            """;

        var result = await ReadAsync(json).ConfigureAwait(true);

        result.Model.ShouldBeNull();
        result.Diagnostics.ShouldContain(
            diagnostic => diagnostic.Code == DiagnosticCode.ModelSchemaViolation);
    }

    private static Task<ModelJsonReadResult> ReadAsync(string json) =>
        ReadAsync(Encoding.UTF8.GetBytes(json));

    private static async Task<ModelJsonReadResult> ReadAsync(byte[] bytes)
    {
        using var source = new MemoryStream(bytes, writable: false);
        return await ModelJsonReader.ReadAsync(source).ConfigureAwait(false);
    }
}
