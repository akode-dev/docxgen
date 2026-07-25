using System.Text.Json;
using Shouldly;
using Xunit;

namespace Akode.DocxGen.Core.Tests.Dependencies;

public sealed class ResolvedPackageLicenseTests
{
    private const string ManifestRelativePath = "eng/package-license-allowlist.json";

    private static readonly string[] ForbiddenPackages =
    [
        "DocxTemplater.Markdown",
        "DocxTemplater.Images",
        "SixLabors.ImageSharp",
    ];

    [Fact]
    public void EveryResolvedPackageVersionHasAnApprovedLicense()
    {
        var manifest = LoadManifest();
        var resolved = LoadResolvedPackages()
            .Select(package => package.Identity)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var approved = manifest.Packages
            .Select(package => package.Identity)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        manifest.Packages
            .Select(package => package.Identity)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ShouldBe(manifest.Packages.Count);
        approved.ShouldBe(resolved, StringComparer.OrdinalIgnoreCase);

        foreach (var package in manifest.Packages)
        {
            manifest.AllowedLicenses.ShouldContain(package.License);
        }
    }

    [Fact]
    public void ForbiddenPackagesAreAbsentFromTheResolvedGraph()
    {
        var offenders = LoadResolvedPackages()
            .Where(package => ForbiddenPackages.Contains(
                package.Id,
                StringComparer.OrdinalIgnoreCase))
            .Select(package => $"{package.Identity} in {package.LockFile}")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        offenders.ShouldBeEmpty();
    }

    private static LicenseManifest LoadManifest()
    {
        var path = Path.Combine(
            RepositoryLayout.Root,
            ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var allowedLicenses = root
            .GetProperty("allowedLicenses")
            .EnumerateArray()
            .Select(element => RequiredString(element, "license"))
            .ToHashSet(StringComparer.Ordinal);
        var packages = root
            .GetProperty("packages")
            .EnumerateArray()
            .Select(element => new ApprovedPackage(
                RequiredString(element.GetProperty("id"), "package id"),
                RequiredString(element.GetProperty("version"), "package version"),
                RequiredString(element.GetProperty("license"), "package license")))
            .ToArray();

        allowedLicenses.ShouldNotBeEmpty();
        packages.ShouldNotBeEmpty();

        return new LicenseManifest(allowedLicenses, packages);
    }

    private static List<ResolvedPackage> LoadResolvedPackages()
    {
        var packages = new List<ResolvedPackage>();

        foreach (var path in Directory.EnumerateFiles(
                     RepositoryLayout.Root,
                     "packages.lock.json",
                     SearchOption.AllDirectories)
                     .Where(path => !RepositoryLayout.IsGeneratedPath(path)))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var framework in document.RootElement
                         .GetProperty("dependencies")
                         .EnumerateObject())
            {
                foreach (var package in framework.Value.EnumerateObject())
                {
                    var type = RequiredString(
                        package.Value.GetProperty("type"),
                        "dependency type");
                    if (type.Equals("Project", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    packages.Add(new ResolvedPackage(
                        package.Name,
                        RequiredString(package.Value.GetProperty("resolved"), "resolved version"),
                        RepositoryLayout.RelativePath(path)));
                }
            }
        }

        packages.ShouldNotBeEmpty();
        return packages;
    }

    private static string RequiredString(JsonElement element, string description) =>
        element.GetString()
        ?? throw new InvalidOperationException(
            $"{ManifestRelativePath} contains a null {description}.");

    private sealed record LicenseManifest(
        IReadOnlySet<string> AllowedLicenses,
        IReadOnlyList<ApprovedPackage> Packages);

    private sealed record ApprovedPackage(string Id, string Version, string License)
    {
        public string Identity => $"{Id}@{Version}";
    }

    private sealed record ResolvedPackage(string Id, string Version, string LockFile)
    {
        public string Identity => $"{Id}@{Version}";
    }
}
