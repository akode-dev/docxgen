using System.Text.Json;
using System.Text.Json.Nodes;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Pipeline;
using Json.Schema;

namespace Akode.DocxGen.Core.Model;

/// <summary>Builds a self-contained structural schema from template analysis.</summary>
internal static class TemplateJsonSchemaGenerator
{
    private const string Draft202012 =
        "https://json-schema.org/draft/2020-12/schema";

    public static GenerateSchemaResult Generate(
        TemplateSchema template,
        string templateName,
        string? explicitId,
        string? explicitVersion)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        var diagnostics = new DiagnosticCollector();
        CopyDiagnostics(template.Diagnostics, diagnostics);
        var roots = template.Roots.Count > 0
            ? template.Roots
            : TemplateShapeFallback.Build(template.Placeholders);
        var templateId = explicitId
            ?? template.TemplateId
            ?? DefaultTemplateId(templateName);
        var templateVersion = explicitVersion
            ?? template.TemplateVersion
            ?? "1.0.0";
        if (roots.Count == 0)
        {
            diagnostics.Add(
                DiagnosticCode.SchemaNoBindings,
                templateName);
            return new GenerateSchemaResult(
                string.Empty,
                templateId,
                templateVersion,
                template.TemplateHash,
                0,
                diagnostics.Items);
        }

        var root = new JsonObject
        {
            ["$schema"] = Draft202012,
            ["$id"] =
                $"urn:akode:docxgen:schema:{templateId}:{templateVersion}",
            ["title"] = $"{templateId} template contract {templateVersion}",
            ["x-docxgen-templateId"] = templateId,
            ["x-docxgen-templateVersion"] = templateVersion,
            ["x-docxgen-templateHash"] = template.TemplateHash,
            ["type"] = "object",
            ["required"] = JsonArrayOf("modelVersion", "template", "data"),
            ["properties"] = BuildEnvelopeProperties(
                roots,
                templateId,
                templateVersion),
            ["additionalProperties"] = false,
            ["$defs"] = BuildDefinitions(),
        };
        var schemaJson = root.ToJsonString(
            new JsonSerializerOptions
            {
                WriteIndented = true,
            }) + "\n";
        _ = JsonSchema.FromText(
            schemaJson,
            new BuildOptions
            {
                SchemaRegistry = new SchemaRegistry(),
            });
        return new GenerateSchemaResult(
            schemaJson,
            templateId,
            templateVersion,
            template.TemplateHash,
            CountBindings(roots),
            diagnostics.Items);
    }

    private static JsonObject BuildEnvelopeProperties(
        IReadOnlyList<TemplateShapeNode> roots,
        string templateId,
        string templateVersion) =>
        new()
        {
            ["$schema"] = new JsonObject
            {
                ["type"] = "string",
            },
            ["modelVersion"] = new JsonObject
            {
                ["const"] = "1.0",
            },
            ["template"] = new JsonObject
            {
                ["type"] = "object",
                ["required"] = JsonArrayOf("id", "version"),
                ["properties"] = new JsonObject
                {
                    ["id"] = new JsonObject
                    {
                        ["const"] = templateId,
                    },
                    ["version"] = new JsonObject
                    {
                        ["const"] = templateVersion,
                    },
                },
                ["additionalProperties"] = false,
            },
            ["options"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["culture"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["minLength"] = 2,
                        ["maxLength"] = 64,
                    },
                    ["strict"] = new JsonObject
                    {
                        ["type"] = "boolean",
                    },
                    ["headingOffset"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["minimum"] = -5,
                        ["maximum"] = 5,
                    },
                    ["allowRawHtml"] = new JsonObject
                    {
                        ["type"] = "boolean",
                    },
                    ["allowRemoteImages"] = new JsonObject
                    {
                        ["type"] = "boolean",
                    },
                    ["updateFieldsOnOpen"] = new JsonObject
                    {
                        ["type"] = "boolean",
                    },
                },
                ["additionalProperties"] = false,
            },
            ["data"] = BuildObjectSchema(roots),
        };

    private static JsonObject BuildDefinitions() =>
        new()
        {
            ["text"] = new JsonObject
            {
                ["oneOf"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "string",
                        ["minLength"] = 1,
                    },
                    new JsonObject
                    {
                        ["type"] = JsonArrayOf("number", "boolean"),
                    },
                    Directive("$text"),
                },
            },
            ["markdown"] = new JsonObject
            {
                ["oneOf"] = new JsonArray
                {
                    Directive("$md", allowEmpty: true),
                    Directive("$mdFile"),
                },
            },
            ["binary"] = Directive("$file"),
        };

    private static JsonObject Directive(
        string name,
        bool allowEmpty = false) =>
        new()
        {
            ["type"] = "object",
            ["required"] = JsonArrayOf(name),
            ["properties"] = new JsonObject
            {
                [name] = new JsonObject
                {
                    ["type"] = "string",
                    ["minLength"] = allowEmpty ? 0 : 1,
                },
            },
            ["additionalProperties"] = false,
        };

    private static JsonObject BuildNodeSchema(TemplateShapeNode node) =>
        node.Kind switch
        {
            ModelValueKind.Markdown => Reference("markdown"),
            ModelValueKind.Binary => Reference("binary"),
            ModelValueKind.Collection => new JsonObject
            {
                ["type"] = "array",
                ["items"] = node.Item is null
                    ? new JsonObject()
                    : BuildNodeSchema(node.Item),
            },
            ModelValueKind.StructuredObject => BuildObjectSchema(node.Properties),
            _ => Reference("text"),
        };

    private static JsonObject BuildObjectSchema(
        IReadOnlyList<TemplateShapeNode> properties)
    {
        var propertySchemas = new JsonObject();
        foreach (var property in properties.OrderBy(
                     item => item.Name,
                     StringComparer.Ordinal))
        {
            propertySchemas[property.Name] = BuildNodeSchema(property);
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = propertySchemas,
            ["additionalProperties"] = false,
        };
    }

    private static JsonObject Reference(string definition) =>
        new()
        {
            ["$ref"] = $"#/$defs/{definition}",
        };

    private static JsonArray JsonArrayOf(params string[] values) =>
        JsonArrayOf(values.AsEnumerable());

    private static JsonArray JsonArrayOf(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static int CountBindings(IEnumerable<TemplateShapeNode> nodes) =>
        nodes.Sum(CountBindings);

    private static int CountBindings(TemplateShapeNode node) =>
        node.Kind switch
        {
            ModelValueKind.StructuredObject => CountBindings(node.Properties),
            ModelValueKind.Collection => 1
                + (node.Item is null ? 0 : CountBindings(node.Item)),
            _ => 1,
        };

    private static string DefaultTemplateId(string templateName)
    {
        var source = Path.GetFileNameWithoutExtension(templateName);
        var builder = new System.Text.StringBuilder(source.Length);
        var previousSeparator = false;
        foreach (var character in source)
        {
            var safe = IsAsciiLetterOrDigit(character)
                || character is '.' or '_' or '-';
            if (safe)
            {
                builder.Append(char.ToLowerInvariant(character));
                previousSeparator = false;
            }
            else if (!previousSeparator)
            {
                builder.Append('-');
                previousSeparator = true;
            }
        }

        var result = builder.ToString().Trim('.', '_', '-');
        if (result.Length == 0)
        {
            return "template";
        }

        if (!IsAsciiLetterOrDigit(result[0]))
        {
            result = "template-" + result;
        }

        return result.Length <= 128 ? result : result[..128].TrimEnd('.', '_', '-');
    }

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9';

    private static void CopyDiagnostics(
        IEnumerable<Diagnostic> source,
        DiagnosticCollector destination)
    {
        foreach (var diagnostic in source)
        {
            destination.Add(diagnostic);
        }
    }

    private static class TemplateShapeFallback
    {
        public static TemplateShapeNode[] Build(
            IEnumerable<TemplatePlaceholder> placeholders)
        {
            var roots = new SortedDictionary<string, MutableNode>(
                StringComparer.Ordinal);
            foreach (var placeholder in placeholders.OrderBy(
                         item => item.Path.Count(character => character == '.')))
            {
                Insert(roots, placeholder);
            }

            return roots.Values.Select(node => node.Freeze()).ToArray();
        }

        private static void Insert(
            IDictionary<string, MutableNode> roots,
            TemplatePlaceholder placeholder)
        {
            var segments = placeholder.Path.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return;
            }

            IDictionary<string, MutableNode> properties = roots;
            MutableNode? node = null;
            for (var index = 0; index < segments.Length; index++)
            {
                if (!properties.TryGetValue(segments[index], out node))
                {
                    node = new MutableNode(segments[index]);
                    properties.Add(segments[index], node);
                }

                if (index == segments.Length - 1)
                {
                    node.Kind = placeholder.Kind;
                    if (placeholder.Kind == ModelValueKind.Collection)
                    {
                        node.Item ??= new MutableNode("item")
                        {
                            Kind = ModelValueKind.StructuredObject,
                        };
                        foreach (var itemProperty in placeholder.ItemProperties)
                        {
                            node.Item.Properties.TryAdd(
                                itemProperty,
                                new MutableNode(itemProperty));
                        }
                    }
                }

                properties = node.Kind == ModelValueKind.Collection
                    ? (node.Item ??= new MutableNode("item")
                    {
                        Kind = ModelValueKind.StructuredObject,
                    }).Properties
                    : node.Properties;
            }
        }

        private sealed class MutableNode(string name)
        {
            public string Name { get; } = name;

            public ModelValueKind Kind { get; set; } =
                ModelValueKind.StructuredObject;

            public SortedDictionary<string, MutableNode> Properties { get; } =
                new(StringComparer.Ordinal);

            public MutableNode? Item { get; set; }

            public TemplateShapeNode Freeze() =>
                new(
                    Name,
                    Kind,
                    Properties.Values.Select(property => property.Freeze()),
                    Item?.Freeze());
        }
    }
}
