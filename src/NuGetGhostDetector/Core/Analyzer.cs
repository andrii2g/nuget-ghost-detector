using NuGetGhostDetector.Infrastructure;
using NuGetGhostDetector.Models;

namespace NuGetGhostDetector.Core;

internal sealed record AnalysisResult(
    AnalysisMode AnalysisMode,
    IReadOnlyList<PackageUsageResult> LikelyUnused,
    IReadOnlyList<PackageUsageResult> PossiblyUnused,
    IReadOnlyList<PackageUsageResult> Used,
    IReadOnlyList<PackageUsageResult> Ignored,
    IReadOnlyList<string> Warnings);

internal sealed class Analyzer
{
    public AnalysisResult Analyze(
        IReadOnlyList<ProjectPackageReference> packages,
        SourceUsageIndex usageIndex,
        AssetsInfo assetsInfo,
        IReadOnlySet<string> ignoredPackages)
    {
        var likelyUnused = new List<PackageUsageResult>();
        var possiblyUnused = new List<PackageUsageResult>();
        var used = new List<PackageUsageResult>();
        var ignored = new List<PackageUsageResult>();

        foreach (var package in packages.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase))
        {
            var result = AnalyzePackage(package, usageIndex, assetsInfo, ignoredPackages);
            switch (result.Classification)
            {
                case PackageClassification.Used:
                    used.Add(result);
                    break;
                case PackageClassification.LikelyUnused:
                    likelyUnused.Add(result);
                    break;
                case PackageClassification.PossiblyUnused:
                    possiblyUnused.Add(result);
                    break;
                case PackageClassification.Ignored:
                    ignored.Add(result);
                    break;
            }
        }

        return new AnalysisResult(assetsInfo.AnalysisMode, likelyUnused, possiblyUnused, used, ignored, assetsInfo.Warnings);
    }

    private static PackageUsageResult AnalyzePackage(
        ProjectPackageReference package,
        SourceUsageIndex usageIndex,
        AssetsInfo assetsInfo,
        IReadOnlySet<string> ignoredPackages)
    {
        if (WellKnownPackageRules.IsIgnoredInfrastructure(package.PackageId, ignoredPackages))
        {
            return new PackageUsageResult(package.PackageId, package.Version, PackageClassification.Ignored, "Ignored package rule matched.", []);
        }

        var hints = assetsInfo.PackageNamespaceHints.TryGetValue(package.PackageId, out var mappedHints)
            ? mappedHints
            : WellKnownPackageRules.GetHints(package.PackageId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var evidence = FindEvidence(hints, usageIndex);
        if (evidence.Count > 0)
        {
            return new PackageUsageResult(package.PackageId, package.Version, PackageClassification.Used, "Static usage evidence found in source files.", evidence);
        }

        if (assetsInfo.AnalysisMode == AnalysisMode.FallbackNoAssets)
        {
            return new PackageUsageResult(package.PackageId, package.Version, PackageClassification.PossiblyUnused, "Assets file missing; fallback heuristics found no direct usage evidence.", []);
        }

        if (assetsInfo.DirectToTransitivePackages.TryGetValue(package.PackageId, out var transitivePackages))
        {
            foreach (var transitivePackage in transitivePackages)
            {
                if (assetsInfo.PackageNamespaceHints.TryGetValue(transitivePackage, out var transitiveHints) &&
                    FindEvidence(transitiveHints, usageIndex).Count > 0)
                {
                    return new PackageUsageResult(package.PackageId, package.Version, PackageClassification.PossiblyUnused, "No direct usage evidence found, but transitive dependency usage was detected.", [transitivePackage]);
                }
            }
        }

        if (WellKnownPackageRules.IsWeakPackage(package.PackageId))
        {
            return new PackageUsageResult(package.PackageId, package.Version, PackageClassification.PossiblyUnused, "Weak package-family match with no direct static evidence.", []);
        }

        return new PackageUsageResult(package.PackageId, package.Version, PackageClassification.LikelyUnused, "No static usage evidence found in C# source files.", []);
    }

    private static List<string> FindEvidence(IEnumerable<string> hints, SourceUsageIndex usageIndex)
    {
        var evidence = new List<string>();

        foreach (var hint in hints.Where(hint => !string.IsNullOrWhiteSpace(hint)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (usageIndex.UsingNamespaces.Any(ns => ns.Equals(hint, StringComparison.OrdinalIgnoreCase) || ns.StartsWith($"{hint}.", StringComparison.OrdinalIgnoreCase)))
            {
                evidence.Add($"using {hint}");
            }
            else if (usageIndex.Namespaces.Any(ns => ns.Equals(hint, StringComparison.OrdinalIgnoreCase) || ns.StartsWith($"{hint}.", StringComparison.OrdinalIgnoreCase)))
            {
                evidence.Add($"namespace {hint}");
            }
            else if (usageIndex.AttributeNames.Contains(hint, StringComparer.OrdinalIgnoreCase))
            {
                evidence.Add($"[{hint}]");
            }
            else if (usageIndex.Tokens.Contains(hint, StringComparer.OrdinalIgnoreCase))
            {
                evidence.Add(hint);
            }
        }

        return evidence.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
