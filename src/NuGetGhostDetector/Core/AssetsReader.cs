using A2G.NuGetGhostDetector.Infrastructure;
using A2G.NuGetGhostDetector.Models;
using System.Text.Json;

namespace A2G.NuGetGhostDetector.Core;

internal sealed record AssetsInfo(
    AnalysisMode AnalysisMode,
    IReadOnlyDictionary<string, IReadOnlySet<string>> PackageNamespaceHints,
    IReadOnlyDictionary<string, IReadOnlySet<string>> DirectToTransitivePackages,
    IReadOnlyList<string> Warnings);

internal sealed class AssetsReader
{
    public AssetsInfo Read(string projectPath, IReadOnlyList<ProjectPackageReference> directPackages, bool verbose)
    {
        var assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
        {
            return BuildFallback(directPackages);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
            var namespaceHints = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
            var transitiveMap = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
            var directIds = directPackages.Select(package => package.PackageId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (document.RootElement.TryGetProperty("targets", out var targetsElement))
            {
                var firstTarget = targetsElement.EnumerateObject().FirstOrDefault();
                if (firstTarget.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var library in firstTarget.Value.EnumerateObject())
                    {
                        var packageId = library.Name.Split('/')[0];
                        var hints = new HashSet<string>(WellKnownPackageRules.GetHints(packageId), StringComparer.OrdinalIgnoreCase);

                        if (library.Value.TryGetProperty("compile", out var compileElement))
                        {
                            AddAssemblyHints(compileElement, hints);
                        }

                        if (library.Value.TryGetProperty("runtime", out var runtimeElement))
                        {
                            AddAssemblyHints(runtimeElement, hints);
                        }

                        namespaceHints[packageId] = hints;

                        if (directIds.Contains(packageId))
                        {
                            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            if (library.Value.TryGetProperty("dependencies", out var dependenciesElement))
                            {
                                foreach (var dependency in dependenciesElement.EnumerateObject())
                                {
                                    dependencies.Add(dependency.Name);
                                }
                            }

                            transitiveMap[packageId] = dependencies;
                        }
                    }
                }
            }

            foreach (var directPackage in directPackages)
            {
                if (!namespaceHints.ContainsKey(directPackage.PackageId))
                {
                    namespaceHints[directPackage.PackageId] = WellKnownPackageRules.GetHints(directPackage.PackageId)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

                if (!transitiveMap.ContainsKey(directPackage.PackageId))
                {
                    transitiveMap[directPackage.PackageId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            return new AssetsInfo(AnalysisMode.Full, namespaceHints, transitiveMap, []);
        }
        catch (Exception ex)
        {
            var fallback = BuildFallback(directPackages);
            if (verbose)
            {
                return fallback with { Warnings = [$"Warning: failed to parse assets file: {assetsPath} ({ex.Message})"] };
            }

            return fallback;
        }
    }

    private static void AddAssemblyHints(JsonElement container, ISet<string> hints)
    {
        foreach (var item in container.EnumerateObject())
        {
            var assemblyName = Path.GetFileNameWithoutExtension(item.Name);
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                continue;
            }

            hints.Add(assemblyName);
            var root = assemblyName.Split('.')[0];
            if (!string.IsNullOrWhiteSpace(root))
            {
                hints.Add(root);
            }
        }
    }

    private static AssetsInfo BuildFallback(IReadOnlyList<ProjectPackageReference> directPackages)
    {
        var hints = directPackages.ToDictionary(
            package => package.PackageId,
            package => (IReadOnlySet<string>)WellKnownPackageRules.GetHints(package.PackageId).ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var transitiveMap = directPackages.ToDictionary(
            package => package.PackageId,
            _ => (IReadOnlySet<string>)new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        return new AssetsInfo(AnalysisMode.FallbackNoAssets, hints, transitiveMap, []);
    }
}
