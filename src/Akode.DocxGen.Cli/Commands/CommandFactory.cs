#pragma warning disable CA2007 // Command handlers run in a console process without a synchronization context.
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Akode.DocxGen.Cli.Output;
using Akode.DocxGen.Core.Diagnostics;
using Akode.DocxGen.Core.Model;
using Akode.DocxGen.Core.Pipeline;
using Akode.DocxGen.Core.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace Akode.DocxGen.Cli.Commands;

internal static class CommandFactory
{
    private static readonly JsonSerializerOptions SchemaJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static Command Inspect(IServiceProvider services)
    {
        var template = RequiredFile("--template", "-t", "Path to the .docx template.");
        var schemaOut = new Option<FileInfo?>("--schema-out")
        {
            Description = "Optional output path for the discovered schema.",
        };
        var includeTextProbe = new Option<bool>("--include-text-probe");
        var json = JsonOption();
        var command = new Command(
            "inspect",
            "Inspect a DOCX template contract without rendering.")
        {
            template,
            schemaOut,
            includeTextProbe,
            json,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var templateFile = parseResult.GetRequiredValue(template);
                await using var stream = OpenRead(templateFile.FullName);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = await pipeline.InspectAsync(
                    new InspectRequest(
                        new InputArtifact(templateFile.FullName, stream),
                        parseResult.GetValue(includeTextProbe)),
                    cancellationToken).ConfigureAwait(false);
                var output = parseResult.GetValue(schemaOut);
                if (output is not null)
                {
                    var serialized = JsonSerializer.Serialize(
                        result.Schema,
                        SchemaJsonOptions);
                    await AtomicFileWriter.WriteTextAsync(
                        output.FullName,
                        $"{serialized}{Environment.NewLine}",
                        overwrite: false,
                        cancellationToken).ConfigureAwait(false);
                }

                timer.Stop();
                var data = new InspectReportData(
                    templateFile.FullName,
                    result.Schema.TemplateId,
                    result.Schema.TemplateVersion,
                    result.Schema.TemplateHash,
                    result.Schema.Placeholders,
                    result.Schema.RequiredStyles,
                    result.UnsupportedForStaticAnalysis,
                    timer.ElapsedMilliseconds);
                if (HasErrors(result.Schema.Diagnostics))
                {
                    return WriteFailure<InspectReportData>(
                        CommandName.Inspect,
                        ExitCode.TemplateError,
                        "Template inspection failed.",
                        result.Schema.Diagnostics,
                        useJson);
                }

                return WriteSuccess(
                    CommandName.Inspect,
                    "Template inspected successfully.",
                    data,
                    result.Schema.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<InspectReportData>(
                    CommandName.Inspect,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    public static Command ScaffoldModel(IServiceProvider services)
    {
        var template = RequiredFile("--template", "-t", "Path to the .docx template.");
        var output = RequiredFile("--out", "-o", "Path for model.json.");
        var withStubs = new Option<bool>("--with-markdown-stubs");
        var force = new Option<bool>("--force");
        var json = JsonOption();
        var command = new Command(
            "scaffold-model",
            "Create an editable JSON model skeleton from a template.")
        {
            template,
            output,
            withStubs,
            force,
            json,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var templateFile = parseResult.GetRequiredValue(template);
                var outputFile = parseResult.GetRequiredValue(output);
                await using var templateStream = OpenRead(templateFile.FullName);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = await pipeline.ScaffoldModelAsync(
                    new ScaffoldModelRequest(
                        new InputArtifact(templateFile.FullName, templateStream),
                        parseResult.GetValue(withStubs)),
                    cancellationToken).ConfigureAwait(false);
                if (HasErrors(result.Diagnostics))
                {
                    return WriteFailure<ScaffoldModelReportData>(
                        CommandName.ScaffoldModel,
                        ExitCode.TemplateError,
                        "Model scaffolding failed.",
                        result.Diagnostics,
                        useJson);
                }

                var overwrite = parseResult.GetValue(force);
                await AtomicFileWriter.WriteTextAsync(
                    outputFile.FullName,
                    result.ModelJson,
                    overwrite,
                    cancellationToken).ConfigureAwait(false);
                var outputDirectory = outputFile.DirectoryName
                    ?? Directory.GetCurrentDirectory();
                foreach (var stub in result.MarkdownStubs)
                {
                    await AtomicFileWriter.WriteTextAsync(
                        Path.Combine(
                            outputDirectory,
                            stub.Key.Replace('/', Path.DirectorySeparatorChar)),
                        stub.Value,
                        overwrite,
                        cancellationToken).ConfigureAwait(false);
                }

                timer.Stop();
                return WriteSuccess(
                    CommandName.ScaffoldModel,
                    "Model scaffold created.",
                    new ScaffoldModelReportData(
                        outputFile.FullName,
                        result.TemplateHash,
                        result.MarkdownStubs.Keys.Order(StringComparer.Ordinal).ToArray(),
                        timer.ElapsedMilliseconds),
                    result.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<ScaffoldModelReportData>(
                    CommandName.ScaffoldModel,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    public static Command ValidateModel(IServiceProvider services)
    {
        var template = RequiredFile("--template", "-t", "Path to the .docx template.");
        var model = new Option<string>("--model", "-m")
        {
            Required = true,
            Description = "Path to model.json, or '-' for stdin.",
        };
        var markdown = OptionalFile("--markdown", "Optional anchored Markdown.");
        var assets = new Option<DirectoryInfo?>("--assets-dir");
        var lenient = new Option<bool>("--lenient");
        var json = JsonOption();
        var command = new Command(
            "validate-model",
            "Validate model, Markdown, assets, and template bindings.")
        {
            template,
            model,
            markdown,
            assets,
            lenient,
            json,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var templateFile = parseResult.GetRequiredValue(template);
                var modelSource = parseResult.GetRequiredValue(model);
                var markdownFile = parseResult.GetValue(markdown);
                await using var templateStream = OpenRead(templateFile.FullName);
                await using var modelStream = OpenInput(modelSource);
                await using var markdownStream = markdownFile is null
                    ? null
                    : OpenRead(markdownFile.FullName);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = await pipeline.ValidateModelAsync(
                    new ValidateModelRequest(
                        new InputArtifact(templateFile.FullName, templateStream),
                        new InputArtifact(InputName(modelSource), modelStream),
                        markdownStream is null
                            ? null
                            : new InputArtifact(markdownFile!.FullName, markdownStream),
                        ResolveAssetsRoot(
                            parseResult.GetValue(assets),
                            modelSource,
                            markdownFile),
                        CreateRenderOptions(
                            strict: !parseResult.GetValue(lenient),
                            overrides: IsExplicit(parseResult, lenient)
                                ? RenderOptionOverrides.Strict
                                : RenderOptionOverrides.None)),
                    cancellationToken).ConfigureAwait(false);
                timer.Stop();
                if (!result.IsValid)
                {
                    return WriteFailure<ValidateModelReportData>(
                        CommandName.ValidateModel,
                        ExitCode.ModelError,
                        "Model validation failed.",
                        result.Diagnostics,
                        useJson);
                }

                return WriteSuccess(
                    CommandName.ValidateModel,
                    "Model is valid.",
                    new ValidateModelReportData(
                        result.TemplateHash,
                        result.ModelHash,
                        result.Model!.BoundPaths,
                        result.Model.UnboundPaths,
                        result.Model.MarkdownStats,
                        timer.ElapsedMilliseconds),
                    result.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<ValidateModelReportData>(
                    CommandName.ValidateModel,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    public static Command Render(IServiceProvider services)
    {
        var template = RequiredFile("--template", "-t", "Path to the .docx template.");
        var output = RequiredFile("--out", "-o", "Output .docx path.");
        var model = new Option<string?>("--model", "-m")
        {
            Description = "Path to model.json, or '-' for stdin.",
        };
        var markdown = OptionalFile("--markdown", "Optional Markdown source.");
        var assets = new Option<DirectoryInfo?>("--assets-dir");
        var lenient = new Option<bool>("--lenient");
        var culture = new Option<string>("--culture")
        {
            DefaultValueFactory = _ => "en-US",
        };
        var headingOffset = new Option<int>("--heading-offset");
        var allowRawHtml = new Option<bool>("--allow-raw-html");
        var allowRemoteImages = new Option<bool>("--allow-remote-images");
        var noUpdateFields = new Option<bool>("--no-update-fields-on-open");
        var set = new Option<string[]>("--set")
        {
            Description = "Highest-precedence model override path=value.",
        };
        var property = new Option<string[]>("--doc-property")
        {
            Description = "Custom document property name=value.",
        };
        var appendVersion = new Option<bool>("--append-document-version");
        var validate = new Option<bool>("--validate");
        var dryRun = new Option<bool>("--dry-run");
        var overwrite = new Option<bool>("--overwrite");
        var json = JsonOption();
        var command = new Command(
            "render",
            "Render a DOCX from a template, model, and Markdown.")
        {
            template,
            output,
            model,
            markdown,
            assets,
            lenient,
            culture,
            headingOffset,
            allowRawHtml,
            allowRemoteImages,
            noUpdateFields,
            set,
            property,
            appendVersion,
            validate,
            dryRun,
            overwrite,
            json,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var templateFile = parseResult.GetRequiredValue(template);
                var outputFile = parseResult.GetRequiredValue(output);
                var modelSource = parseResult.GetValue(model);
                var markdownFile = parseResult.GetValue(markdown);
                if (modelSource is null && markdownFile is null)
                {
                    throw new ArgumentException(
                        "render requires --model, --markdown, or both.");
                }

                await using var templateStream = OpenRead(templateFile.FullName);
                await using var modelStream = modelSource is null
                    ? null
                    : OpenInput(modelSource);
                await using var markdownStream = markdownFile is null
                    ? null
                    : OpenRead(markdownFile.FullName);
                var optionOverrides = RenderOptionOverrides.None;
                AddOverride(
                    ref optionOverrides,
                    RenderOptionOverrides.Strict,
                    IsExplicit(parseResult, lenient));
                AddOverride(
                    ref optionOverrides,
                    RenderOptionOverrides.Culture,
                    IsExplicit(parseResult, culture));
                AddOverride(
                    ref optionOverrides,
                    RenderOptionOverrides.HeadingOffset,
                    IsExplicit(parseResult, headingOffset));
                AddOverride(
                    ref optionOverrides,
                    RenderOptionOverrides.AllowRawHtml,
                    IsExplicit(parseResult, allowRawHtml));
                AddOverride(
                    ref optionOverrides,
                    RenderOptionOverrides.AllowRemoteImages,
                    IsExplicit(parseResult, allowRemoteImages));
                AddOverride(
                    ref optionOverrides,
                    RenderOptionOverrides.UpdateFieldsOnOpen,
                    IsExplicit(parseResult, noUpdateFields));
                var options = CreateRenderOptions(
                    strict: !parseResult.GetValue(lenient),
                    parseResult.GetRequiredValue(culture),
                    parseResult.GetValue(headingOffset),
                    parseResult.GetValue(allowRawHtml),
                    parseResult.GetValue(allowRemoteImages),
                    !parseResult.GetValue(noUpdateFields),
                    optionOverrides);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = await pipeline.RenderAsync(
                    new RenderRequest(
                        new InputArtifact(templateFile.FullName, templateStream),
                        modelStream is null
                            ? null
                            : new InputArtifact(
                                InputName(modelSource!),
                                modelStream),
                        markdownStream is null
                            ? null
                            : new InputArtifact(
                                markdownFile!.FullName,
                                markdownStream),
                        ResolveAssetsRoot(
                            parseResult.GetValue(assets),
                            modelSource,
                            markdownFile),
                        options,
                        ParsePairs(parseResult.GetValue(set), "--set"),
                        ParsePairs(
                            parseResult.GetValue(property),
                            "--doc-property"),
                        parseResult.GetValue(validate),
                        parseResult.GetValue(dryRun)),
                    cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return WriteFailure<RenderReportData>(
                        CommandName.Render,
                        MapRenderExitCode(result.Diagnostics),
                        "Document rendering failed.",
                        result.Diagnostics,
                        useJson);
                }

                var finalPath = outputFile.FullName;
                if (parseResult.GetValue(appendVersion))
                {
                    if (string.IsNullOrWhiteSpace(result.DocumentVersion))
                    {
                        throw new ArgumentException(
                            "--append-document-version requires data.ds.Document.Version.");
                    }

                    finalPath = OutputFileNaming.AppendVersion(
                        finalPath,
                        result.DocumentVersion);
                }

                long outputBytes = 0;
                if (!result.DryRun)
                {
                    var document = result.Document
                        ?? throw new InvalidDataException(
                            "A successful render returned no document stream.");
                    outputBytes = document.CanSeek ? document.Length : 0;
                    await AtomicFileWriter.WriteStreamAsync(
                        finalPath,
                        document,
                        parseResult.GetValue(overwrite),
                        cancellationToken).ConfigureAwait(false);
                    await document.DisposeAsync().ConfigureAwait(false);
                }

                timer.Stop();
                return WriteSuccess(
                    CommandName.Render,
                    result.DryRun
                        ? "Render preflight succeeded."
                        : "Document rendered successfully.",
                    new RenderReportData(
                        result.DryRun ? null : finalPath,
                        outputBytes,
                        result.TemplateHash,
                        result.ModelHash,
                        timer.ElapsedMilliseconds,
                        result.BoundPaths,
                        result.UnboundPaths,
                        result.MarkdownStats,
                        Summary(result.Validation),
                        result.DryRun,
                        result.DocumentVersion),
                    result.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<RenderReportData>(
                    CommandName.Render,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    public static Command Convert(IServiceProvider services)
    {
        var markdown = RequiredFile("--markdown", null, "Input Markdown path.");
        var output = RequiredFile("--out", "-o", "Output .docx path.");
        var styleReference = OptionalFile(
            "--style-reference",
            "Optional DOCX style reference.");
        var headingOffset = new Option<int>("--heading-offset");
        var toc = new Option<bool>("--toc");
        var validate = new Option<bool>("--validate");
        var overwrite = new Option<bool>("--overwrite");
        var json = JsonOption();
        var command = new Command(
            "convert",
            "Convert Markdown to a standalone DOCX.")
        {
            markdown,
            output,
            styleReference,
            headingOffset,
            toc,
            validate,
            overwrite,
            json,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var markdownFile = parseResult.GetRequiredValue(markdown);
                var outputFile = parseResult.GetRequiredValue(output);
                var styleFile = parseResult.GetValue(styleReference);
                await using var markdownStream = OpenRead(markdownFile.FullName);
                await using var styleStream = styleFile is null
                    ? null
                    : OpenRead(styleFile.FullName);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = await pipeline.ConvertAsync(
                    new ConvertRequest(
                        new InputArtifact(markdownFile.FullName, markdownStream),
                        styleStream is null
                            ? null
                            : new InputArtifact(styleFile!.FullName, styleStream),
                        parseResult.GetValue(headingOffset),
                        parseResult.GetValue(toc),
                        parseResult.GetValue(validate)),
                    cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return WriteFailure<ConvertReportData>(
                        CommandName.Convert,
                        ExitCode.RenderError,
                        "Markdown conversion failed.",
                        result.Diagnostics,
                        useJson);
                }

                var outputBytes = result.Document.CanSeek
                    ? result.Document.Length
                    : 0;
                await AtomicFileWriter.WriteStreamAsync(
                    outputFile.FullName,
                    result.Document,
                    parseResult.GetValue(overwrite),
                    cancellationToken).ConfigureAwait(false);
                await result.Document.DisposeAsync().ConfigureAwait(false);
                timer.Stop();
                return WriteSuccess(
                    CommandName.Convert,
                    "Markdown converted successfully.",
                    new ConvertReportData(
                        outputFile.FullName,
                        outputBytes,
                        timer.ElapsedMilliseconds,
                        result.MarkdownStats,
                        Summary(result.Validation)),
                    result.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<ConvertReportData>(
                    CommandName.Convert,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    public static Command Extract(IServiceProvider services)
    {
        var file = RequiredFile("--file", null, "Input .docx path.");
        var output = RequiredFile("--out", "-o", "Output Markdown path.");
        var assetsDirectory = new Option<DirectoryInfo?>("--assets-dir")
        {
            Description =
                "Directory for embedded images; defaults to <output-name>.assets.",
        };
        var overwrite = new Option<bool>("--overwrite");
        var json = JsonOption();
        var command = new Command(
            "extract",
            "Extract semantic Markdown and embedded images from a DOCX.")
        {
            file,
            output,
            assetsDirectory,
            overwrite,
            json,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var documentFile = parseResult.GetRequiredValue(file);
                var outputFile = parseResult.GetRequiredValue(output);
                var outputPath = Path.GetFullPath(outputFile.FullName);
                var outputRoot = Path.GetDirectoryName(outputPath)
                    ?? throw new IOException(
                        $"Output '{outputPath}' has no parent directory.");
                var explicitAssets = parseResult.GetValue(assetsDirectory);
                var assetsPath = explicitAssets?.FullName
                    ?? Path.Combine(
                        outputRoot,
                        $"{Path.GetFileNameWithoutExtension(outputPath)}.assets");
                assetsPath = Path.GetFullPath(assetsPath);
                var imagePrefix = Path.GetRelativePath(outputRoot, assetsPath)
                    .Replace('\\', '/');
                if (Path.IsPathRooted(imagePrefix))
                {
                    throw new ArgumentException(
                        "--assets-dir must be on the same file-system root as --out.");
                }

                await using var documentStream = OpenRead(documentFile.FullName);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = await pipeline.ExtractAsync(
                    new ExtractRequest(
                        new InputArtifact(documentFile.FullName, documentStream),
                        imagePrefix),
                    cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return WriteFailure<ExtractReportData>(
                        CommandName.Extract,
                        ExitCode.RenderError,
                        "DOCX extraction failed.",
                        result.Diagnostics,
                        useJson);
                }

                var overwriteFiles = parseResult.GetValue(overwrite);
                var assetPaths = result.Assets
                    .Select(asset => Path.Combine(assetsPath, asset.FileName))
                    .ToArray();
                EnsureExtractionOutputsAvailable(
                    outputPath,
                    assetsPath,
                    assetPaths,
                    overwriteFiles);
                foreach (var pair in result.Assets.Zip(assetPaths))
                {
                    using var assetStream = new MemoryStream(
                        pair.First.Content.ToArray(),
                        writable: false);
                    await AtomicFileWriter.WriteStreamAsync(
                        pair.Second,
                        assetStream,
                        overwriteFiles,
                        cancellationToken).ConfigureAwait(false);
                }

                await AtomicFileWriter.WriteTextAsync(
                    outputPath,
                    result.Markdown,
                    overwriteFiles,
                    cancellationToken).ConfigureAwait(false);
                timer.Stop();
                return WriteSuccess(
                    CommandName.Extract,
                    "DOCX extracted successfully.",
                    new ExtractReportData(
                        outputPath,
                        Encoding.UTF8.GetByteCount(result.Markdown),
                        result.Assets.Count == 0 ? null : assetsPath,
                        assetPaths,
                        timer.ElapsedMilliseconds,
                        result.Stats),
                    result.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<ExtractReportData>(
                    CommandName.Extract,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    public static Command Validate(IServiceProvider services)
    {
        var file = RequiredFile("--file", null, "DOCX file to validate.");
        var failOn = new Option<string>("--fail-on")
        {
            DefaultValueFactory = _ => "error",
        };
        var maxErrors = new Option<int>("--max-errors")
        {
            DefaultValueFactory = _ => 50,
        };
        var json = JsonOption();
        var command = new Command(
            "validate",
            "Validate an existing DOCX package.")
        {
            file,
            failOn,
            maxErrors,
            json,
        };
        command.SetAction(parseResult =>
        {
            var useJson = parseResult.GetValue(json);
            var timer = Stopwatch.StartNew();
            try
            {
                var documentFile = parseResult.GetRequiredValue(file);
                var threshold = parseResult.GetRequiredValue(failOn);
                if (threshold is not ("warning" or "error"))
                {
                    throw new ArgumentException(
                        "--fail-on must be 'warning' or 'error'.");
                }

                using var stream = OpenRead(documentFile.FullName);
                var pipeline = services.GetRequiredService<DocxGenPipeline>();
                var result = pipeline.ValidateDocument(
                    new ValidateDocumentRequest(
                        new InputArtifact(documentFile.FullName, stream),
                        parseResult.GetValue(maxErrors)));
                timer.Stop();
                var summary = Summary(result.Validation)!;
                var fails = !result.Validation.IsValid
                    || threshold == "warning"
                    && result.Validation.Diagnostics.Any(
                        diagnostic =>
                            diagnostic.Severity == DiagnosticSeverity.Warning);
                if (fails)
                {
                    return WriteFailure<ValidateDocumentReportData>(
                        CommandName.Validate,
                        ExitCode.ValidationError,
                        "Document validation failed.",
                        EnsureError(result.Validation.Diagnostics),
                        useJson);
                }

                return WriteSuccess(
                    CommandName.Validate,
                    "Document is valid.",
                    new ValidateDocumentReportData(
                        documentFile.FullName,
                        timer.ElapsedMilliseconds,
                        summary),
                    result.Validation.Diagnostics,
                    useJson);
            }
            catch (Exception exception) when (IsHandled(exception))
            {
                return WriteException<ValidateDocumentReportData>(
                    CommandName.Validate,
                    exception,
                    useJson);
            }
        });
        return command;
    }

    private static Option<FileInfo> RequiredFile(
        string name,
        string? alias,
        string description)
    {
        var option = alias is null
            ? new Option<FileInfo>(name)
            : new Option<FileInfo>(name, alias);
        option.Required = true;
        option.Description = description;
        return option;
    }

    private static Option<FileInfo?> OptionalFile(
        string name,
        string description) =>
        new(name) { Description = description };

    private static Option<bool> JsonOption() =>
        new("--json") { Description = "Write a versioned JSON report to stdout." };

    private static FileStream OpenRead(string path) =>
        new(
            Path.GetFullPath(path),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

    private static Stream OpenInput(string source) =>
        source == "-" ? CliOutput.StandardInput() : OpenRead(source);

    private static string InputName(string source) =>
        source == "-" ? "-" : Path.GetFullPath(source);

    private static string ResolveAssetsRoot(
        DirectoryInfo? explicitRoot,
        string? model,
        FileInfo? markdown)
    {
        if (explicitRoot is not null)
        {
            return explicitRoot.FullName;
        }

        if (model is not null && model != "-")
        {
            return Path.GetDirectoryName(Path.GetFullPath(model))
                ?? Directory.GetCurrentDirectory();
        }

        return markdown?.DirectoryName ?? Directory.GetCurrentDirectory();
    }

    private static void EnsureExtractionOutputsAvailable(
        string output,
        string assetsDirectory,
        string[] assets,
        bool overwrite)
    {
        if (assets.Length > 0 && File.Exists(assetsDirectory))
        {
            throw new IOException(
                $"Assets directory '{assetsDirectory}' is an existing file.");
        }

        foreach (var path in assets.Prepend(output))
        {
            if (Directory.Exists(path))
            {
                throw new IOException(
                    $"Output '{path}' is an existing directory.");
            }

            if (!overwrite && File.Exists(path))
            {
                throw new IOException(
                    $"Output '{path}' already exists. Use --overwrite or choose another path.");
            }
        }
    }

    private static RenderOptions CreateRenderOptions(
        bool strict,
        string culture = "en-US",
        int headingOffset = 0,
        bool allowRawHtml = false,
        bool allowRemoteImages = false,
        bool updateFieldsOnOpen = true,
        RenderOptionOverrides overrides = RenderOptionOverrides.All) =>
        new()
        {
            Culture = culture,
            Strict = strict,
            HeadingOffset = headingOffset,
            AllowRawHtml = allowRawHtml,
            AllowRemoteImages = allowRemoteImages,
            UpdateFieldsOnOpen = updateFieldsOnOpen,
            Overrides = overrides,
        };

    private static bool IsExplicit(ParseResult result, Option option) =>
        result.GetResult(option) is { Implicit: false };

    private static void AddOverride(
        ref RenderOptionOverrides overrides,
        RenderOptionOverrides value,
        bool enabled)
    {
        if (enabled)
        {
            overrides |= value;
        }
    }

    private static Dictionary<string, string> ParsePairs(
        string[]? values,
        string optionName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var value in values ?? [])
        {
            var separator = value.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                throw new ArgumentException(
                    $"{optionName} value '{value}' must use name=value syntax.");
            }

            var key = value[..separator];
            var pairValue = value[(separator + 1)..];
            if (!result.TryAdd(key, pairValue))
            {
                throw new ArgumentException(
                    $"{optionName} contains duplicate key '{key}'.");
            }
        }

        return result;
    }

    private static int WriteSuccess<TData>(
        string command,
        string message,
        TData data,
        IEnumerable<Diagnostic> diagnostics,
        bool json)
        where TData : class
    {
        var report = CommandReport.Success(
            command,
            message,
            data,
            diagnostics);
        CliOutput.WriteReport(report, json);
        return (int)ExitCode.Success;
    }

    private static int WriteFailure<TData>(
        string command,
        ExitCode exitCode,
        string message,
        IEnumerable<Diagnostic> diagnostics,
        bool json)
        where TData : class
    {
        var report = CommandReport.Failure<TData>(
            command,
            exitCode,
            message,
            diagnostics);
        CliOutput.WriteReport(report, json);
        return (int)exitCode;
    }

    private static int WriteException<TData>(
        string command,
        Exception exception,
        bool json)
        where TData : class
    {
        var exceptionMessage = string.Equals(
            Environment.GetEnvironmentVariable("DOCXGEN_DEBUG"),
            "1",
            StringComparison.Ordinal)
            ? exception.ToString()
            : exception.Message;
        var (exitCode, diagnostic) = exception switch
        {
            ArgumentException => (
                ExitCode.UsageError,
                    DiagnosticRegistry.Create(
                        DiagnosticCode.InvalidUsage,
                    message: exceptionMessage)),
            IOException or UnauthorizedAccessException => (
                ExitCode.IoError,
                    DiagnosticRegistry.Create(
                        DiagnosticCode.IoFailure,
                    message: exceptionMessage)),
            InvalidDataException or InvalidOperationException => (
                ExitCode.RenderError,
                    DiagnosticRegistry.Create(
                        DiagnosticCode.RenderFailure,
                    message: exceptionMessage)),
            _ => (
                ExitCode.UnexpectedError,
                    DiagnosticRegistry.Create(
                        DiagnosticCode.UnexpectedFailure,
                    message: exceptionMessage)),
        };
        return WriteFailure<TData>(
            command,
            exitCode,
            "Command failed.",
            [diagnostic],
            json);
    }

    private static bool IsHandled(Exception exception) =>
        exception is not (
            OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or OperationCanceledException);

    private static bool HasErrors(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Any(
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    private static ExitCode MapRenderExitCode(
        IReadOnlyList<Diagnostic> diagnostics)
    {
        var first = diagnostics.FirstOrDefault(
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (first?.Code.StartsWith("E-TPL-", StringComparison.Ordinal) == true)
        {
            return ExitCode.TemplateError;
        }

        if (first?.Code.StartsWith("E-OUT-", StringComparison.Ordinal) == true)
        {
            return ExitCode.ValidationError;
        }

        return first?.Code.StartsWith("E-MDL-", StringComparison.Ordinal) == true
               || first?.Code.StartsWith("E-MD-", StringComparison.Ordinal) == true
               || first?.Code.StartsWith("E-SEC-", StringComparison.Ordinal) == true
            ? ExitCode.ModelError
            : ExitCode.RenderError;
    }

    private static DocumentValidationSummary? Summary(ValidationReport? report)
    {
        if (report is null)
        {
            return null;
        }

        return new DocumentValidationSummary(
            report.IsValid,
            report.Diagnostics.Count(
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            report.Diagnostics.Count(
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning));
    }

    private static IReadOnlyList<Diagnostic> EnsureError(
        IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics.Any(
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return diagnostics;
        }

        return diagnostics
            .Append(
                DiagnosticRegistry.Create(
                    DiagnosticCode.OutputInvalidOoxml,
                    message: "Validation warnings met the configured failure threshold."))
            .ToArray();
    }
}
#pragma warning restore CA2007
