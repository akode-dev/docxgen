using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Akode.DocxGen.Core.Diagnostics;
using Json.Schema;

namespace Akode.DocxGen.Core.Model;

/// <summary>
/// Reads UTF-8 model JSON, validates the base schema, and materializes typed
/// model directives without resolving local files.
/// </summary>
public static class ModelJsonReader
{
    private const string SchemaResourceName =
        "Akode.DocxGen.Core.Schemas.docxgen-model-1.0.schema.json";

    private static readonly Lazy<JsonSchema> BaseSchema = new(
        LoadBaseSchema,
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly HashSet<string> KnownDirectives =
        new(StringComparer.Ordinal)
        {
            "$file",
            "$md",
            "$mdFile",
            "$text",
        };

    /// <summary>Reads and validates one model stream without taking ownership.</summary>
    public static async Task<ModelJsonReadResult> ReadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("The model stream must be readable.", nameof(source));
        }

        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var bytes = buffer.ToArray();
        var hash = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";
        var diagnostics = new DiagnosticCollector();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(WithoutUtf8Bom(bytes));
        }
        catch (JsonException exception)
        {
            diagnostics.Add(
                DiagnosticCode.ModelInvalidJson,
                NormalizeJsonPath(exception.Path),
                FormatJsonError(exception));
            return new ModelJsonReadResult(null, hash, diagnostics.Items);
        }

        using (document)
        {
            FindDuplicateProperties(document.RootElement, "/", diagnostics);
            if (diagnostics.HasErrors)
            {
                return new ModelJsonReadResult(null, hash, diagnostics.Items);
            }

            FindUnknownDirectives(document.RootElement, diagnostics);
            if (diagnostics.HasErrors)
            {
                return new ModelJsonReadResult(null, hash, diagnostics.Items);
            }

            AddSchemaDiagnostics(document.RootElement, diagnostics);
            if (diagnostics.HasErrors)
            {
                return new ModelJsonReadResult(null, hash, diagnostics.Items);
            }

            return new ModelJsonReadResult(
                ParseDocument(document.RootElement),
                hash,
                diagnostics.Items);
        }
    }

    private static ReadOnlyMemory<byte> WithoutUtf8Bom(byte[] bytes) =>
        bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF
            ? bytes.AsMemory(3)
            : bytes;

    private static string FormatJsonError(JsonException exception)
    {
        var location = exception.LineNumber is null
            ? string.Empty
            : $" at line {exception.LineNumber + 1}, byte {exception.BytePositionInLine + 1}";
        return $"The model is not valid JSON{location}.";
    }

    private static void FindDuplicateProperties(
        JsonElement element,
        string path,
        DiagnosticCollector diagnostics)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = AppendPointer(path, property.Name);
                if (!names.Add(property.Name))
                {
                    diagnostics.Add(
                        DiagnosticCode.ModelInvalidJson,
                        propertyPath,
                        $"The JSON object contains duplicate property '{property.Name}'.",
                        "Remove the duplicate property so each object key occurs exactly once.");
                }

                FindDuplicateProperties(property.Value, propertyPath, diagnostics);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                FindDuplicateProperties(
                    item,
                    AppendPointer(path, index.ToString(CultureInfo.InvariantCulture)),
                    diagnostics);
                index++;
            }
        }
    }

    private static void FindUnknownDirectives(
        JsonElement root,
        DiagnosticCollector diagnostics)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("data", out var data))
        {
            return;
        }

        FindUnknownDirectives(data, "/data", diagnostics);
    }

    private static void FindUnknownDirectives(
        JsonElement element,
        string path,
        DiagnosticCollector diagnostics)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = AppendPointer(path, property.Name);
                if (property.Name.StartsWith('$')
                    && !KnownDirectives.Contains(property.Name))
                {
                    diagnostics.Add(
                        DiagnosticCode.ModelUnknownDirective,
                        propertyPath,
                        $"The model contains unknown directive '{property.Name}'.",
                        "Use only $md, $mdFile, $file, or $text directives.");
                }

                FindUnknownDirectives(property.Value, propertyPath, diagnostics);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                FindUnknownDirectives(
                    item,
                    AppendPointer(path, index.ToString(CultureInfo.InvariantCulture)),
                    diagnostics);
                index++;
            }
        }
    }

    private static void AddSchemaDiagnostics(
        JsonElement root,
        DiagnosticCollector diagnostics)
    {
        var results = BaseSchema.Value.Evaluate(
            root,
            new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
                RequireFormatValidation = true,
            });
        if (results.IsValid)
        {
            return;
        }

        var failures = results.Details is { Count: > 0 } details
            ? details.Where(
                result => !result.IsValid && result.Errors is { Count: > 0 })
            : [results];
        var emitted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var failure in failures)
        {
            var path = NormalizeJsonPath(failure.InstanceLocation.ToString());
            if (failure.Errors is not { } errors)
            {
                continue;
            }

            foreach (var error in errors)
            {
                var message = $"Schema rule '{error.Key}' failed: {error.Value}";
                if (emitted.Add($"{path}\0{message}"))
                {
                    diagnostics.Add(
                        DiagnosticCode.ModelSchemaViolation,
                        path,
                        message,
                        "Update the reported value to satisfy the base DocxGen model schema.");
                }
            }
        }

        if (!diagnostics.HasErrors)
        {
            diagnostics.Add(DiagnosticCode.ModelSchemaViolation, "/");
        }
    }

    private static ModelDocument ParseDocument(JsonElement root)
    {
        var template = root.GetProperty("template");
        var options = root.TryGetProperty("options", out var optionsElement)
            ? ParseOptions(optionsElement)
            : ModelDocumentOptions.Default;
        var data = root.GetProperty("data")
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => ParseValue(property.Value),
                StringComparer.Ordinal);

        return new ModelDocument(
            root.GetProperty("modelVersion").GetString()!,
            new TemplateReference(
                template.GetProperty("id").GetString()!,
                template.GetProperty("version").GetString()!),
            options,
            new ReadOnlyDictionary<string, ModelValue>(data));
    }

    private static ModelDocumentOptions ParseOptions(JsonElement options) =>
        new(
            GetString(options, "culture", ModelDocumentOptions.Default.Culture),
            GetBoolean(options, "strict", ModelDocumentOptions.Default.Strict),
            GetInteger(options, "headingOffset", ModelDocumentOptions.Default.HeadingOffset),
            GetBoolean(options, "allowRawHtml", ModelDocumentOptions.Default.AllowRawHtml),
            GetBoolean(
                options,
                "allowRemoteImages",
                ModelDocumentOptions.Default.AllowRemoteImages),
            GetBoolean(
                options,
                "updateFieldsOnOpen",
                ModelDocumentOptions.Default.UpdateFieldsOnOpen));

    private static string GetString(JsonElement element, string name, string fallback) =>
        element.TryGetProperty(name, out var value) ? value.GetString()! : fallback;

    private static bool GetBoolean(JsonElement element, string name, bool fallback) =>
        element.TryGetProperty(name, out var value) ? value.GetBoolean() : fallback;

    private static int GetInteger(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) ? value.GetInt32() : fallback;

    private static ModelValue ParseValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return new CollectionModelValue(value.EnumerateArray().Select(ParseValue));
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return new PrimitiveModelValue(value);
        }

        if (value.TryGetProperty("$md", out var markdown))
        {
            return new InlineMarkdownModelValue(markdown.GetString()!);
        }

        if (value.TryGetProperty("$mdFile", out var markdownFile))
        {
            return new MarkdownFileModelValue(markdownFile.GetString()!);
        }

        if (value.TryGetProperty("$file", out var file))
        {
            return new FileModelValue(file.GetString()!);
        }

        if (value.TryGetProperty("$text", out var text))
        {
            return new PlainTextModelValue(text.GetString()!);
        }

        return new ObjectModelValue(
            value.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ParseValue(property.Value),
                StringComparer.Ordinal));
    }

    private static JsonSchema LoadBaseSchema()
    {
        using var stream = typeof(ModelJsonReader).Assembly.GetManifestResourceStream(
            SchemaResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded schema '{SchemaResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    private static string AppendPointer(string path, string token)
    {
        var escaped = token.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
        return path == "/" ? $"/{escaped}" : $"{path}/{escaped}";
    }

    private static string NormalizeJsonPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "#")
        {
            return "/";
        }

        return path.StartsWith('#') ? path[1..] : path;
    }
}
