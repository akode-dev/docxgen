using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Akode.DocxGen.Core.Abstractions;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Markdown;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Security;

namespace Akode.DocxGen.Core.Pipeline;

/// <summary>Coordinates all technology-neutral document operations.</summary>
public sealed class DocxGenPipeline
{
    private readonly ITemplateInspector templateInspector;
    private readonly IDocumentRenderer documentRenderer;
    private readonly IMarkdownDocumentConverter markdownConverter;
    private readonly IDocxMarkdownExtractor markdownExtractor;
    private readonly IOoxmlValidator ooxmlValidator;
    private readonly IReadOnlyList<IDocumentPostProcessor> postProcessors;

    /// <summary>Initializes the complete pipeline from adapter boundaries.</summary>
    public DocxGenPipeline(
        ITemplateInspector templateInspector,
        IDocumentRenderer documentRenderer,
        IMarkdownDocumentConverter markdownConverter,
        IDocxMarkdownExtractor markdownExtractor,
        IOoxmlValidator ooxmlValidator,
        IEnumerable<IDocumentPostProcessor> postProcessors)
    {
        this.templateInspector =
            templateInspector ?? throw new ArgumentNullException(nameof(templateInspector));
        this.documentRenderer =
            documentRenderer ?? throw new ArgumentNullException(nameof(documentRenderer));
        this.markdownConverter =
            markdownConverter ?? throw new ArgumentNullException(nameof(markdownConverter));
        this.markdownExtractor =
            markdownExtractor ?? throw new ArgumentNullException(nameof(markdownExtractor));
        this.ooxmlValidator =
            ooxmlValidator ?? throw new ArgumentNullException(nameof(ooxmlValidator));
        ArgumentNullException.ThrowIfNull(postProcessors);
        this.postProcessors = new ReadOnlyCollection<IDocumentPostProcessor>(
            postProcessors.OrderBy(processor => processor.Order).ToArray());
    }

    /// <summary>Inspects a DOCX template without rendering it.</summary>
    public async Task<InspectResult> InspectAsync(
        InspectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var bytes = await ReadAllAsync(
            request.Template.Content,
            cancellationToken).ConfigureAwait(false);
        using var stream = new MemoryStream(bytes, writable: false);
        var inspected = templateInspector.Inspect(stream, cancellationToken);
        var schema = TemplateSchemaMetadata.Apply(
            inspected,
            request.Template.Name);
        return new InspectResult(
            schema,
            [
                "Runtime expressions and conditional reachability are reported as a union.",
                "Subtemplates and dynamic table formatters require runtime validation.",
            ]);
    }

    /// <summary>Generates a structural Draft 2020-12 schema from a template.</summary>
    public async Task<GenerateSchemaResult> GenerateSchemaAsync(
        GenerateSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var bytes = await ReadAllAsync(
            request.Template.Content,
            cancellationToken).ConfigureAwait(false);
        using var stream = new MemoryStream(bytes, writable: false);
        var inspected = templateInspector.Inspect(stream, cancellationToken);
        return TemplateJsonSchemaGenerator.Generate(
            inspected,
            request.Template.Name,
            request.TemplateId,
            request.TemplateVersion);
    }

    /// <summary>Creates a model scaffold from an inspected template.</summary>
    public async Task<ScaffoldModelResult> ScaffoldModelAsync(
        ScaffoldModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inspection = await InspectAsync(
            new InspectRequest(request.Template),
            cancellationToken).ConfigureAwait(false);
        var stubs = new Dictionary<string, string>(StringComparer.Ordinal);
        var data = new JsonObject();
        if (inspection.Schema.Roots.Count > 0)
        {
            foreach (var root in inspection.Schema.Roots)
            {
                data[root.Name] = CreateShapeValue(
                    root,
                    root.Name,
                    request.WithMarkdownStubs,
                    stubs);
            }
        }
        else
        {
            foreach (var placeholder in inspection.Schema.Placeholders)
            {
                InsertPlaceholder(
                    data,
                    placeholder,
                    request.WithMarkdownStubs,
                    stubs);
            }
        }

        var envelope = new JsonObject
        {
            ["$schema"] = "./docxgen-model-1.0.schema.json",
            ["modelVersion"] = "1.0",
            ["template"] = new JsonObject
            {
                ["id"] = inspection.Schema.TemplateId
                    ?? Path.GetFileNameWithoutExtension(request.Template.Name),
                ["version"] = inspection.Schema.TemplateVersion ?? "1.0.0",
            },
            ["options"] = new JsonObject
            {
                ["culture"] = "en-US",
                ["strict"] = true,
                ["headingOffset"] = 0,
            },
            ["data"] = data,
        };
        var modelJson = envelope.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        return new ScaffoldModelResult(
            inspection.Schema.TemplateHash,
            $"{modelJson}{Environment.NewLine}",
            new ReadOnlyDictionary<string, string>(stubs),
            inspection.Schema.Diagnostics);
    }

    /// <summary>Validates model, Markdown, assets, and template bindings.</summary>
    public async Task<ValidateModelResult> ValidateModelAsync(
        ValidateModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var prepared = await PrepareAsync(
            request.Template,
            request.Model,
            request.Markdown,
            request.AssetsRoot,
            request.Options,
            overrides: null,
            cancellationToken).ConfigureAwait(false);
        return new ValidateModelResult(
            prepared.Schema.TemplateHash,
            prepared.ModelHash ?? EmptyHash,
            prepared.Model,
            prepared.Diagnostics);
    }

    /// <summary>Runs preflight, rendering, post-processing, and optional validation.</summary>
    public async Task<RenderResult> RenderAsync(
        RenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var prepared = await PrepareAsync(
            request.Template,
            request.Model,
            request.Markdown,
            request.AssetsRoot,
            request.Options,
            request.Overrides,
            cancellationToken).ConfigureAwait(false);
        if (!prepared.IsValid || request.DryRun)
        {
            return new RenderResult(
                Document: null,
                prepared.Schema.TemplateHash,
                prepared.ModelHash,
                prepared.Model?.BoundPaths ?? [],
                prepared.Model?.UnboundPaths ?? [],
                prepared.Model?.MarkdownStats ?? MarkdownStats.Empty,
                Validation: null,
                prepared.Diagnostics,
                request.DryRun)
            {
                DocumentVersion = prepared.Model?.DocumentVersion,
            };
        }

        using var template = new MemoryStream(prepared.TemplateBytes, writable: false);
        var outcome = await documentRenderer.RenderAsync(
            template,
            prepared.Model!,
            prepared.Options,
            cancellationToken).ConfigureAwait(false);
        var diagnostics = new DiagnosticCollector();
        CopyDiagnostics(prepared.Diagnostics, diagnostics);
        CopyDiagnostics(outcome.Diagnostics, diagnostics);
        var document = EnsureEditableMemoryStream(outcome.Document);
        foreach (var processor in postProcessors)
        {
            document.Position = 0;
            processor.Apply(
                document,
                new PostProcessOptions
                {
                    UpdateFieldsOnOpen = prepared.Options.UpdateFieldsOnOpen,
                    DocumentProperties = request.DocumentProperties,
                },
                diagnostics);
        }

        ValidationReport? validation = null;
        if (request.ValidateOutput)
        {
            document.Position = 0;
            validation = ooxmlValidator.Validate(document);
            CopyDiagnostics(validation.Diagnostics, diagnostics);
        }

        document.Position = 0;
        return new RenderResult(
            document,
            prepared.Schema.TemplateHash,
            prepared.ModelHash,
            outcome.BoundPaths,
            outcome.UnboundPaths,
            outcome.MarkdownStats,
            validation,
            diagnostics.Items,
            DryRun: false)
        {
            DocumentVersion = prepared.Model?.DocumentVersion,
        };
    }

    /// <summary>Creates a standalone document from Markdown.</summary>
    public Task<ConvertResult> ConvertAsync(
        ConvertRequest request,
        CancellationToken cancellationToken = default) =>
        markdownConverter.ConvertAsync(request, cancellationToken);

    /// <summary>Extracts semantic Markdown and embedded assets from a DOCX.</summary>
    public Task<ExtractResult> ExtractAsync(
        ExtractRequest request,
        CancellationToken cancellationToken = default) =>
        markdownExtractor.ExtractAsync(request, cancellationToken);

    /// <summary>Validates an existing DOCX package.</summary>
    public ValidateDocumentResult ValidateDocument(ValidateDocumentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ValidateDocumentResult(
            ooxmlValidator.Validate(request.Document.Content, request.MaxErrors));
    }

    private async Task<PreparedModel> PrepareAsync(
        InputArtifact template,
        InputArtifact? model,
        InputArtifact? markdown,
        string assetsRoot,
        RenderOptions options,
        IReadOnlyDictionary<string, string>? overrides,
        CancellationToken cancellationToken)
    {
        var templateBytes = await ReadAllAsync(
            template.Content,
            cancellationToken).ConfigureAwait(false);
        var inspection = await InspectAsync(
            new InspectRequest(
                new InputArtifact(
                    template.Name,
                    new MemoryStream(templateBytes, writable: false))),
            cancellationToken).ConfigureAwait(false);
        var diagnostics = new DiagnosticCollector();
        CopyDiagnostics(inspection.Schema.Diagnostics, diagnostics);

        ModelDocument? document = null;
        string? modelHash = null;
        ReadOnlyMemory<byte> modelBytes = default;
        if (model is not null)
        {
            modelBytes = await ReadAllAsync(
                model.Content,
                cancellationToken).ConfigureAwait(false);
            using var modelStream = new MemoryStream(modelBytes.ToArray(), writable: false);
            var read = await ModelJsonReader.ReadAsync(
                modelStream,
                cancellationToken).ConfigureAwait(false);
            document = read.Model;
            modelHash = read.ModelHash;
            CopyDiagnostics(read.Diagnostics, diagnostics);
            if (read.IsValid)
            {
                CopyDiagnostics(
                    TemplateModelValidator.Validate(modelBytes, template.Name),
                    diagnostics);
                CheckTemplateIdentity(document!, inspection.Schema, diagnostics);
            }
        }

        var markdownText = markdown is null
            ? null
            : Encoding.UTF8.GetString(
                await ReadAllAsync(
                    markdown.Content,
                    cancellationToken).ConfigureAwait(false));
        if (modelHash is null && markdownText is not null)
        {
            modelHash = Hash(Encoding.UTF8.GetBytes(markdownText));
        }

        if (diagnostics.HasErrors)
        {
            return new PreparedModel(
                templateBytes,
                inspection.Schema,
                modelHash,
                null,
                options,
                diagnostics.Items);
        }

        var effectiveOptions = ResolveOptions(options, document?.Options);
        var anchors = CreateAnchors(markdownText);
        var merged = ModelSourceMerger.Merge(document?.Data, anchors, overrides);
        CopyDiagnostics(merged.Diagnostics, diagnostics);
        var resolver = new LocalAssetResolver(
            assetsRoot,
            effectiveOptions.Limits);
        var binding = ModelBinder.Bind(
            merged.Data,
            inspection.Schema,
            effectiveOptions,
            resolver);
        CopyDiagnostics(binding.Diagnostics, diagnostics);
        return new PreparedModel(
            templateBytes,
            inspection.Schema,
            modelHash,
            binding.Model,
            effectiveOptions,
            diagnostics.Items);
    }

    private static RenderOptions ResolveOptions(
        RenderOptions caller,
        ModelDocumentOptions? model)
    {
        if (model is null)
        {
            return caller;
        }

        var overrides = caller.Overrides;
        return caller with
        {
            Culture = overrides.HasFlag(RenderOptionOverrides.Culture)
                ? caller.Culture
                : model.Culture,
            Strict = overrides.HasFlag(RenderOptionOverrides.Strict)
                ? caller.Strict
                : model.Strict,
            HeadingOffset = overrides.HasFlag(RenderOptionOverrides.HeadingOffset)
                ? caller.HeadingOffset
                : model.HeadingOffset,
            AllowRawHtml = overrides.HasFlag(RenderOptionOverrides.AllowRawHtml)
                ? caller.AllowRawHtml
                : model.AllowRawHtml,
            AllowRemoteImages =
                overrides.HasFlag(RenderOptionOverrides.AllowRemoteImages)
                    ? caller.AllowRemoteImages
                    : model.AllowRemoteImages,
            UpdateFieldsOnOpen =
                overrides.HasFlag(RenderOptionOverrides.UpdateFieldsOnOpen)
                    ? caller.UpdateFieldsOnOpen
                    : model.UpdateFieldsOnOpen,
        };
    }

    private static SectionAnchorParseResult CreateAnchors(string? markdown)
    {
        if (markdown is null)
        {
            return new SectionAnchorParseResult([], []);
        }

        if (markdown.Contains(
                "docxgen:section",
                StringComparison.OrdinalIgnoreCase))
        {
            return SectionAnchorParser.Parse(markdown);
        }

        return new SectionAnchorParseResult(
            [
                new AnchoredMarkdownSection(
                    "ds.Body",
                    markdown,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    anchorLine: 1),
            ],
            []);
    }

    private static void CheckTemplateIdentity(
        ModelDocument model,
        TemplateSchema schema,
        DiagnosticCollector diagnostics)
    {
        if (schema.TemplateId is not null
            && !string.Equals(
                model.Template.Id,
                schema.TemplateId,
                StringComparison.Ordinal))
        {
            diagnostics.Add(
                DiagnosticCode.TemplateIdentityMismatch,
                "/template/id",
                $"Model template id '{model.Template.Id}' does not match '{schema.TemplateId}'.");
        }

        if (schema.TemplateVersion is not null
            && !string.Equals(
                model.Template.Version,
                schema.TemplateVersion,
                StringComparison.Ordinal))
        {
            diagnostics.Add(
                DiagnosticCode.TemplateIdentityMismatch,
                "/template/version",
                $"Model template version '{model.Template.Version}' does not match "
                + $"'{schema.TemplateVersion}'.");
        }
    }

    private static void InsertPlaceholder(
        JsonObject data,
        TemplatePlaceholder placeholder,
        bool withMarkdownStubs,
        IDictionary<string, string> stubs)
    {
        var segments = placeholder.Path.Split(
            '.',
            StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return;
        }

        var target = data;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (target[segments[index]] is not JsonObject next)
            {
                next = new JsonObject();
                target[segments[index]] = next;
            }

            target = next;
        }

        var leaf = segments[^1];
        target[leaf] = placeholder.Kind switch
        {
            ModelValueKind.Markdown when withMarkdownStubs =>
                CreateMarkdownStub(leaf, stubs),
            ModelValueKind.Markdown => new JsonObject { ["$md"] = string.Empty },
            ModelValueKind.Collection => CreateCollectionItem(placeholder.ItemProperties),
            ModelValueKind.Binary => new JsonObject { ["$file"] = string.Empty },
            _ => string.Empty,
        };
    }

    private static JsonObject CreateMarkdownStub(
        string leaf,
        IDictionary<string, string> stubs)
    {
        var fileName = $"sections/{ToKebabCase(leaf)}.md";
        stubs[fileName] = $"# {leaf}{Environment.NewLine}{Environment.NewLine}";
        return new JsonObject { ["$mdFile"] = fileName };
    }

    private static JsonNode CreateShapeValue(
        TemplateShapeNode node,
        string path,
        bool withMarkdownStubs,
        IDictionary<string, string> stubs)
    {
        switch (node.Kind)
        {
            case ModelValueKind.Markdown when withMarkdownStubs:
            {
                var fileName = $"sections/{ToKebabCase(node.Name)}.md";
                if (stubs.ContainsKey(fileName))
                {
                    fileName =
                        $"sections/{ToKebabCase(path.Replace('.', '-'))}.md";
                }

                stubs[fileName] =
                    $"# {node.Name}{Environment.NewLine}{Environment.NewLine}";
                return new JsonObject { ["$mdFile"] = fileName };
            }

            case ModelValueKind.Markdown:
                return new JsonObject { ["$md"] = string.Empty };
            case ModelValueKind.Binary:
                return new JsonObject { ["$file"] = string.Empty };
            case ModelValueKind.Collection:
                return new JsonArray(
                    node.Item is null
                        ? new JsonObject()
                        : CreateShapeValue(
                            node.Item,
                            path,
                            withMarkdownStubs,
                            stubs));
            case ModelValueKind.StructuredObject:
            {
                var result = new JsonObject();
                foreach (var property in node.Properties)
                {
                    result[property.Name] = CreateShapeValue(
                        property,
                        $"{path}.{property.Name}",
                        withMarkdownStubs,
                        stubs);
                }

                return result;
            }

            default:
                return JsonValue.Create(string.Empty);
        }
    }

    private static JsonArray CreateCollectionItem(IReadOnlyList<string> properties)
    {
        var item = new JsonObject();
        foreach (var property in properties)
        {
            item[property] = string.Empty;
        }

        return new JsonArray(item);
    }

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private static async Task<byte[]> ReadAllAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static MemoryStream EnsureEditableMemoryStream(Stream source)
    {
        if (source is MemoryStream memory && memory.CanWrite)
        {
            memory.Position = 0;
            return memory;
        }

        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var copy = new MemoryStream();
        source.CopyTo(copy);
        source.Dispose();
        copy.Position = 0;
        return copy;
    }

    private static void CopyDiagnostics(
        IEnumerable<Diagnostic> source,
        DiagnosticCollector destination)
    {
        foreach (var diagnostic in source)
        {
            destination.Add(diagnostic);
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";

    private static readonly string EmptyHash = Hash([]);

    private sealed record PreparedModel(
        byte[] TemplateBytes,
        TemplateSchema Schema,
        string? ModelHash,
        BoundModel? Model,
        RenderOptions Options,
        IReadOnlyList<Diagnostic> Diagnostics)
    {
        public bool IsValid =>
            Model is not null
            && Diagnostics.All(
                diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
    }
}
