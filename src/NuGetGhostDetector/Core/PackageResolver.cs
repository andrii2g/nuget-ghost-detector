using System.Xml.Linq;
using NuGetGhostDetector.Infrastructure;
using NuGetGhostDetector.Models;

namespace NuGetGhostDetector.Core;

internal sealed record CompileInputs(
    string ProjectPath,
    bool EnableDefaultCompileItems,
    IReadOnlyList<(string DeclaringDirectory, string Include)> Includes,
    IReadOnlyList<(string DeclaringDirectory, string Remove)> Removes);

internal sealed record PackageResolutionResult(
    IReadOnlyList<ProjectPackageReference> DirectPackages,
    CompileInputs CompileInputs,
    IReadOnlyList<string> Warnings);

internal sealed class PackageResolver
{
    private const int MaxImportDepth = 5;

    public PackageResolutionResult Resolve(string projectPath, string rootPath, bool verbose)
    {
        var warnings = new List<string>();
        var visitedImports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packageCandidates = new Dictionary<string, ProjectPackageReference>(StringComparer.OrdinalIgnoreCase);
        var includes = new List<(string DeclaringDirectory, string Include)>();
        var removes = new List<(string DeclaringDirectory, string Remove)>();
        var enableDefaultCompileItems = true;

        ParseFile(projectPath, PackageSourceKind.ProjectFile, 0);

        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        foreach (var directoryBuildFile in EnumerateDirectoryBuildFiles(projectDirectory, rootPath))
        {
            ParseFile(directoryBuildFile.Path, directoryBuildFile.SourceKind, 0);
        }

        return new PackageResolutionResult(
            packageCandidates.Values.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase).ToArray(),
            new CompileInputs(projectPath, enableDefaultCompileItems, includes, removes),
            warnings);

        void ParseFile(string filePath, PackageSourceKind sourceKind, int depth)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
                var declaringDirectory = Path.GetDirectoryName(filePath)!;

                foreach (var propertyGroup in document.Root?.Elements().Where(e => e.Name.LocalName == "PropertyGroup") ?? [])
                {
                    var compileItemsSetting = propertyGroup.Elements().FirstOrDefault(e => e.Name.LocalName == "EnableDefaultCompileItems")?.Value;
                    if (bool.TryParse(compileItemsSetting, out var parsed) && !parsed)
                    {
                        enableDefaultCompileItems = false;
                    }
                }

                foreach (var itemGroup in document.Root?.Elements().Where(e => e.Name.LocalName == "ItemGroup") ?? [])
                {
                    foreach (var item in itemGroup.Elements())
                    {
                        if (item.Name.LocalName == "PackageReference")
                        {
                            var packageId = item.Attribute("Include")?.Value;
                            if (string.IsNullOrWhiteSpace(packageId))
                            {
                                packageId = item.Attribute("Update")?.Value;
                            }

                            if (!string.IsNullOrWhiteSpace(packageId))
                            {
                                var version = item.Attribute("Version")?.Value ?? item.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
                                UpsertPackage(new ProjectPackageReference(projectPath, packageId, version, filePath, sourceKind));
                            }
                        }
                        else if (item.Name.LocalName == "Compile")
                        {
                            var include = item.Attribute("Include")?.Value;
                            if (!string.IsNullOrWhiteSpace(include))
                            {
                                includes.Add((declaringDirectory, include));
                            }

                            var remove = item.Attribute("Remove")?.Value;
                            if (!string.IsNullOrWhiteSpace(remove))
                            {
                                removes.Add((declaringDirectory, remove));
                            }
                        }
                    }
                }

                if (depth >= MaxImportDepth)
                {
                    return;
                }

                foreach (var import in document.Descendants().Where(e => e.Name.LocalName == "Import"))
                {
                    var importPath = import.Attribute("Project")?.Value;
                    if (string.IsNullOrWhiteSpace(importPath) || !FileSystemHelpers.IsSimpleLiteralPath(importPath))
                    {
                        if (verbose && !string.IsNullOrWhiteSpace(importPath))
                        {
                            warnings.Add($"Warning: skipped unsupported import path: {importPath} in {filePath}");
                        }
                        continue;
                    }

                    var resolvedImportPath = Path.IsPathRooted(importPath)
                        ? Path.GetFullPath(importPath)
                        : Path.GetFullPath(Path.Combine(declaringDirectory, importPath));

                    if (visitedImports.Add(resolvedImportPath))
                    {
                        ParseFile(resolvedImportPath, PackageSourceKind.ImportedFile, depth + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    warnings.Add($"Warning: failed to parse project file: {filePath} ({ex.Message})");
                }
            }
        }

        void UpsertPackage(ProjectPackageReference candidate)
        {
            if (packageCandidates.TryGetValue(candidate.PackageId, out var existing))
            {
                if (candidate.SourceKind < existing.SourceKind)
                {
                    packageCandidates[candidate.PackageId] = candidate;
                }
                else if (candidate.SourceKind == existing.SourceKind &&
                         string.IsNullOrWhiteSpace(existing.Version) &&
                         !string.IsNullOrWhiteSpace(candidate.Version))
                {
                    packageCandidates[candidate.PackageId] = candidate;
                }
            }
            else
            {
                packageCandidates[candidate.PackageId] = candidate;
            }
        }
    }

    private static IEnumerable<(string Path, PackageSourceKind SourceKind)> EnumerateDirectoryBuildFiles(string projectDirectory, string rootPath)
    {
        var current = new DirectoryInfo(projectDirectory);
        var stopAt = new DirectoryInfo(rootPath).FullName;

        while (current is not null)
        {
            var propsPath = Path.Combine(current.FullName, "Directory.Build.props");
            if (File.Exists(propsPath))
            {
                yield return (propsPath, PackageSourceKind.DirectoryBuild);
            }

            var targetsPath = Path.Combine(current.FullName, "Directory.Build.targets");
            if (File.Exists(targetsPath))
            {
                yield return (targetsPath, PackageSourceKind.DirectoryBuild);
            }

            if (string.Equals(current.FullName, stopAt, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = current.Parent;
        }
    }
}
