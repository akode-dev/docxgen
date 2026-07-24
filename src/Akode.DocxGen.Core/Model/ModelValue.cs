using System.Collections.ObjectModel;
using System.Text.Json;

namespace Akode.DocxGen.Core.Model;

/// <summary>A typed value parsed from the recursive model data tree.</summary>
public abstract record ModelValue(ModelValueKind Kind);

/// <summary>A JSON string, number, boolean, or null value.</summary>
public sealed record PrimitiveModelValue : ModelValue
{
    /// <summary>Initializes a primitive value and detaches it from its source document.</summary>
    public PrimitiveModelValue(JsonElement value)
        : base(ModelValueKind.Text)
    {
        if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
        {
            throw new ArgumentException(
                "A primitive model value cannot be an object or array.",
                nameof(value));
        }

        Value = value.Clone();
    }

    /// <summary>Gets the detached JSON primitive.</summary>
    public JsonElement Value { get; }
}

/// <summary>An explicit plain-text directive.</summary>
public sealed record PlainTextModelValue(string Text)
    : ModelValue(ModelValueKind.Text);

/// <summary>An inline Markdown directive.</summary>
public sealed record InlineMarkdownModelValue(string Markdown)
    : ModelValue(ModelValueKind.Markdown);

/// <summary>A local Markdown-file directive.</summary>
public sealed record MarkdownFileModelValue(string RelativePath)
    : ModelValue(ModelValueKind.Markdown);

/// <summary>A local binary-file directive.</summary>
public sealed record FileModelValue(string RelativePath)
    : ModelValue(ModelValueKind.Binary);

/// <summary>An ordered model collection.</summary>
public sealed record CollectionModelValue : ModelValue
{
    /// <summary>Initializes an immutable collection snapshot.</summary>
    public CollectionModelValue(IEnumerable<ModelValue> items)
        : base(ModelValueKind.Collection)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = new ReadOnlyCollection<ModelValue>(items.ToArray());
    }

    /// <summary>Gets the ordered item values.</summary>
    public IReadOnlyList<ModelValue> Items { get; }
}

/// <summary>A structured model object.</summary>
public sealed record ObjectModelValue : ModelValue
{
    /// <summary>Initializes an immutable property snapshot.</summary>
    public ObjectModelValue(IReadOnlyDictionary<string, ModelValue> properties)
        : base(ModelValueKind.StructuredObject)
    {
        ArgumentNullException.ThrowIfNull(properties);
        Properties = new ReadOnlyDictionary<string, ModelValue>(
            new Dictionary<string, ModelValue>(properties, StringComparer.Ordinal));
    }

    /// <summary>Gets case-sensitive object properties.</summary>
    public IReadOnlyDictionary<string, ModelValue> Properties { get; }
}
