using System.Text.Json;
using Akode.DocxGen.Core.Diagnostics;

namespace Akode.DocxGen.Core.Model;

/// <summary>Reads and applies contract metadata from an adjacent JSON Schema.</summary>
public static class TemplateSchemaMetadata
{
    /// <summary>Returns identity metadata when an adjacent file is available.</summary>
    public static (string? Id, string? Version) Read(string templateName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        var schemaPath = Path.ChangeExtension(templateName, ".schema.json");
        if (!File.Exists(schemaPath))
        {
            return (null, null);
        }

        using var document = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        var root = document.RootElement;
        return (
            GetString(root, "x-docxgen-templateId"),
            GetString(root, "x-docxgen-templateVersion"));
    }

    /// <summary>
    /// Applies adjacent identity, hash, and required-path metadata to inspection.
    /// </summary>
    public static TemplateSchema Apply(
        TemplateSchema inspected,
        string templateName)
    {
        ArgumentNullException.ThrowIfNull(inspected);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        var schemaPath = Path.ChangeExtension(templateName, ".schema.json");
        if (!File.Exists(schemaPath))
        {
            return inspected with
            {
                Placeholders = inspected.Placeholders
                    .Select(placeholder => placeholder with { Required = false })
                    .ToArray(),
            };
        }

        var diagnostics = new DiagnosticCollector();
        foreach (var diagnostic in inspected.Diagnostics)
        {
            diagnostics.Add(diagnostic);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
            var root = document.RootElement;
            var requiredPaths = new HashSet<string>(StringComparer.Ordinal);
            CollectDataContracts(root, root, requiredPaths);
            var expectedHash = GetString(root, "x-docxgen-templateHash");
            if (expectedHash is not null
                && !string.Equals(
                    expectedHash,
                    inspected.TemplateHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(
                    DiagnosticCode.TemplateIdentityMismatch,
                    "/x-docxgen-templateHash",
                    $"Adjacent schema hash '{expectedHash}' does not match "
                    + $"template hash '{inspected.TemplateHash}'.",
                    "Regenerate or deliberately update the adjacent schema metadata.");
            }

            return inspected with
            {
                TemplateId =
                    GetString(root, "x-docxgen-templateId")
                    ?? inspected.TemplateId,
                TemplateVersion =
                    GetString(root, "x-docxgen-templateVersion")
                    ?? inspected.TemplateVersion,
                Placeholders = inspected.Placeholders
                    .Select(
                        placeholder => placeholder with
                        {
                            Required = requiredPaths.Contains(placeholder.Path),
                        })
                    .ToArray(),
                Diagnostics = diagnostics.Items,
            };
        }
        catch (Exception exception) when (
            exception is JsonException
            or IOException
            or InvalidOperationException)
        {
            diagnostics.Add(
                DiagnosticCode.TemplateSyntaxError,
                schemaPath,
                $"The adjacent template schema could not be read: {exception.Message}",
                "Correct the adjacent .schema.json file and run inspect again.");
            return inspected with
            {
                Placeholders = inspected.Placeholders
                    .Select(placeholder => placeholder with { Required = false })
                    .ToArray(),
                Diagnostics = diagnostics.Items,
            };
        }
    }

    private static void CollectDataContracts(
        JsonElement schema,
        JsonElement root,
        ISet<string> requiredPaths)
    {
        schema = ResolveLocalReference(schema, root);
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (schema.TryGetProperty("properties", out var properties)
            && properties.ValueKind == JsonValueKind.Object
            && properties.TryGetProperty("data", out var data))
        {
            CollectRequiredPaths(
                data,
                root,
                prefix: string.Empty,
                parentRequired: true,
                requiredPaths);
        }

        if (schema.TryGetProperty("allOf", out var allOf)
            && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in allOf.EnumerateArray())
            {
                CollectDataContracts(child, root, requiredPaths);
            }
        }
    }

    private static void CollectRequiredPaths(
        JsonElement schema,
        JsonElement root,
        string prefix,
        bool parentRequired,
        ISet<string> requiredPaths)
    {
        schema = ResolveLocalReference(schema, root);
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (schema.TryGetProperty("allOf", out var allOf)
            && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in allOf.EnumerateArray())
            {
                CollectRequiredPaths(
                    child,
                    root,
                    prefix,
                    parentRequired,
                    requiredPaths);
            }
        }

        var required = schema.TryGetProperty("required", out var requiredArray)
                       && requiredArray.ValueKind == JsonValueKind.Array
            ? requiredArray.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!)
                .ToHashSet(StringComparer.Ordinal)
            : [];
        if (!schema.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in properties.EnumerateObject())
        {
            var path = string.IsNullOrEmpty(prefix)
                ? property.Name
                : $"{prefix}.{property.Name}";
            var isRequired =
                parentRequired && required.Contains(property.Name);
            if (isRequired)
            {
                requiredPaths.Add(path);
            }

            CollectRequiredPaths(
                property.Value,
                root,
                path,
                isRequired,
                requiredPaths);
        }
    }

    private static JsonElement ResolveLocalReference(
        JsonElement schema,
        JsonElement root)
    {
        if (schema.ValueKind != JsonValueKind.Object
            || !schema.TryGetProperty("$ref", out var referenceElement)
            || referenceElement.ValueKind != JsonValueKind.String)
        {
            return schema;
        }

        var reference = referenceElement.GetString();
        if (reference is null
            || !reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return schema;
        }

        var current = root;
        foreach (var rawSegment in reference[2..].Split('/'))
        {
            var segment = rawSegment
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                return schema;
            }
        }

        return current;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
