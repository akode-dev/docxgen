using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Architecture;

public sealed class ProjectDependencyTests
{
    private static readonly string[] DocxImplementationPackages =
    [
        "DocxTemplater",
        "DocumentFormat.OpenXml",
    ];

    private static readonly string[] RejectedProductionPackages =
    [
        "DocxTemplater.Images",
        "DocxTemplater.Markdown",
        "SixLabors.ImageSharp",
    ];

    [Fact]
    public void ProductionProjectGraphMatchesTheArchitecture()
    {
        var projects = LoadProjects(Path.Combine(RepositoryLayout.Root, "src"));

        AssertProject(
            projects,
            "Akode.DocxGen.Core",
            projectReferences: [],
            packageReferences: ["Markdig"]);
        AssertProject(
            projects,
            "Akode.DocxGen.Docx",
            projectReferences: ["Akode.DocxGen.Core"],
            packageReferences: DocxImplementationPackages);
        AssertProject(
            projects,
            "Akode.DocxGen.Cli",
            projectReferences: ["Akode.DocxGen.Core", "Akode.DocxGen.Docx"],
            packageReferences:
            [
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.Logging.Console",
                "Microsoft.Extensions.Options",
                "System.CommandLine",
            ]);
        AssertProject(
            projects,
            "Akode.DocxGen.Mcp",
            projectReferences: ["Akode.DocxGen.Core"],
            packageReferences: []);
    }

    [Fact]
    public void NoProductionProjectReferencesCli()
    {
        var offenders = LoadProjects(Path.Combine(RepositoryLayout.Root, "src"))
            .Values
            .Where(project => project.ProjectReferences.Contains(
                "Akode.DocxGen.Cli",
                StringComparer.Ordinal))
            .Select(project => project.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void DocxImplementationPackagesAreConfinedToTheDocxAdapter()
    {
        var offenders = LoadProjects(Path.Combine(RepositoryLayout.Root, "src"))
            .Values
            .SelectMany(project => project.PackageReferences.Select(package => (project, package)))
            .Where(reference =>
                DocxImplementationPackages.Contains(reference.package, StringComparer.Ordinal)
                && reference.project.Name != "Akode.DocxGen.Docx")
            .Select(reference => $"{reference.project.Name}: {reference.package}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void RejectedPackagesAreAbsentFromProductionProjects()
    {
        var offenders = LoadProjects(Path.Combine(RepositoryLayout.Root, "src"))
            .Values
            .SelectMany(project => project.PackageReferences.Select(package => (project, package)))
            .Where(reference => RejectedProductionPackages.Contains(
                reference.package,
                StringComparer.Ordinal))
            .Select(reference => $"{reference.project.Name}: {reference.package}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void EveryPackageVersionIsCentrallyManaged()
    {
        var projects = LoadProjects(RepositoryLayout.Root);
        var projectPackageIds = projects
            .Values
            .SelectMany(project => project.PackageReferences)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var centralPackageIds = LoadCentralPackageIds()
            .Order(StringComparer.Ordinal)
            .ToArray();
        var versionedReferences = projects
            .Values
            .SelectMany(project => project.VersionedPackageReferences)
            .Order(StringComparer.Ordinal)
            .ToArray();

        versionedReferences.ShouldBeEmpty();
        projectPackageIds.ShouldBe(centralPackageIds);
    }

    [Fact]
    public void ConsoleWritesAreConfinedToCliOutputAdapters()
    {
        var sourceRoot = Path.Combine(RepositoryLayout.Root, "src");
        var offenders = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains(
                "Console.",
                StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativePath)
            .Where(path => !path.StartsWith(
                "src/Akode.DocxGen.Cli/Output/",
                StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    private static void AssertProject(
        IReadOnlyDictionary<string, ProjectModel> projects,
        string name,
        string[] projectReferences,
        string[] packageReferences)
    {
        projects.ShouldContainKey(name);
        projects[name].ProjectReferences.ShouldBe(
            projectReferences.Order(StringComparer.Ordinal).ToArray());
        projects[name].PackageReferences.ShouldBe(
            packageReferences.Order(StringComparer.Ordinal).ToArray());
    }

    private static Dictionary<string, ProjectModel> LoadProjects(string root) =>
        Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Select(LoadProject)
            .ToDictionary(project => project.Name, StringComparer.Ordinal);

    private static ProjectModel LoadProject(string path)
    {
        var document = XDocument.Load(path, LoadOptions.None);
        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(element => RequiredInclude(element, path))
            .Select(reference => Path.GetFileNameWithoutExtension(reference)
                ?? throw new InvalidOperationException(
                    $"Could not resolve a project name from '{reference}'."))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var packageElements = document.Descendants("PackageReference").ToArray();
        var packageReferences = packageElements
            .Select(element => RequiredInclude(element, path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var versionedPackageReferences = packageElements
            .Where(element =>
                element.Attribute("Version") is not null
                || element.Attribute("VersionOverride") is not null
                || element.Element("Version") is not null
                || element.Element("VersionOverride") is not null)
            .Select(element =>
                $"{RepositoryLayout.RelativePath(path)}: {RequiredInclude(element, path)}")
            .ToArray();

        return new ProjectModel(
            Path.GetFileNameWithoutExtension(path),
            projectReferences,
            packageReferences,
            versionedPackageReferences);
    }

    private static string[] LoadCentralPackageIds()
    {
        var path = Path.Combine(RepositoryLayout.Root, "Directory.Packages.props");
        var document = XDocument.Load(path, LoadOptions.None);

        return document
            .Descendants("PackageVersion")
            .Select(element => RequiredInclude(element, path))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string RequiredInclude(XElement element, string path) =>
        element.Attribute("Include")?.Value
        ?? throw new InvalidOperationException(
            $"{RepositoryLayout.RelativePath(path)} contains an item without Include.");

    private sealed record ProjectModel(
        string Name,
        string[] ProjectReferences,
        string[] PackageReferences,
        string[] VersionedPackageReferences);
}
