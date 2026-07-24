using System.Text.Json;
using System.Text.Json.Nodes;
using Akode.DocxGen.Core.Diagnostics;
using Json.Schema;

namespace Akode.DocxGen.Core.Model;

/// <summary>Validates model JSON against an adjacent template-specific schema.</summary>
public static class TemplateModelValidator
{
    /// <summary>Returns template-specific schema diagnostics when a schema exists.</summary>
    public static IReadOnlyList<Diagnostic> Validate(
        ReadOnlyMemory<byte> modelJson,
        string templateName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        var schemaPath = Path.ChangeExtension(templateName, ".schema.json");
        if (!File.Exists(schemaPath))
        {
            return [];
        }

        var diagnostics = new DiagnosticCollector();
        try
        {
            using var model = JsonDocument.Parse(modelJson);
            var schema = LoadWithoutBaseReference(schemaPath);
            var results = schema.Evaluate(
                model.RootElement,
                new EvaluationOptions
                {
                    OutputFormat = OutputFormat.List,
                    RequireFormatValidation = true,
                });
            AddFailures(results, diagnostics);
        }
        catch (Exception exception) when (
            exception is JsonException
            or InvalidOperationException
            or IOException
            or RefResolutionException)
        {
            diagnostics.Add(
                DiagnosticCode.TemplateSyntaxError,
                schemaPath,
                $"The adjacent template schema could not be evaluated: {exception.Message}",
                "Correct the adjacent .schema.json file and run inspect again.");
        }

        return diagnostics.Items;
    }

    private static JsonSchema LoadWithoutBaseReference(string schemaPath)
    {
        var root = JsonNode.Parse(File.ReadAllText(schemaPath))?.AsObject()
            ?? throw new JsonException("The template schema root is empty.");
        // JsonSchema.Net registers absolute $id values process-wide. Removing the
        // identifier keeps repeated validation of the same adjacent schema isolated
        // and avoids a duplicate-registration failure in long-running CLI/MCP hosts.
        root.Remove("$id");
        if (root["allOf"] is JsonArray allOf)
        {
            for (var index = allOf.Count - 1; index >= 0; index--)
            {
                var reference = allOf[index]?["$ref"]?.GetValue<string>();
                if (reference?.EndsWith(
                        "docxgen-model-1.0.schema.json",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    allOf.RemoveAt(index);
                }
            }
        }

        return JsonSchema.FromText(root.ToJsonString());
    }

    private static void AddFailures(
        EvaluationResults results,
        DiagnosticCollector diagnostics)
    {
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
            var path = NormalizePath(failure.InstanceLocation.ToString());
            if (failure.Errors is not { } errors)
            {
                continue;
            }

            foreach (var error in errors)
            {
                var message = $"Template schema rule '{error.Key}' failed: {error.Value}";
                if (emitted.Add($"{path}\0{message}"))
                {
                    diagnostics.Add(
                        DiagnosticCode.ModelSchemaViolation,
                        path,
                        message,
                        "Update the reported value to satisfy the template-specific schema.");
                }
            }
        }

        if (!diagnostics.HasErrors)
        {
            diagnostics.Add(DiagnosticCode.ModelSchemaViolation, "/");
        }
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "#")
        {
            return "/";
        }

        return path.StartsWith('#') ? path[1..] : path;
    }
}
