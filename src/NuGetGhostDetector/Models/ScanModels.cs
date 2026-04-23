namespace NuGetGhostDetector.Models;

internal enum AnalysisMode
{
    Full,
    FallbackNoAssets
}

internal sealed record ProjectScanResult(
    string Name,
    string Path,
    AnalysisMode AnalysisMode,
    IReadOnlyList<PackageUsageResult> LikelyUnused,
    IReadOnlyList<PackageUsageResult> PossiblyUnused,
    IReadOnlyList<PackageUsageResult> Used,
    IReadOnlyList<PackageUsageResult> Ignored,
    IReadOnlyList<string> Warnings);

internal sealed record ScanResult(
    string InputPath,
    AnalysisMode AnalysisMode,
    IReadOnlyList<ProjectScanResult> Projects,
    IReadOnlyList<string> Warnings)
{
    public int TotalLikelyUnused => Projects.Sum(project => project.LikelyUnused.Count);
    public int TotalPossiblyUnused => Projects.Sum(project => project.PossiblyUnused.Count);
    public int TotalUsed => Projects.Sum(project => project.Used.Count);
    public int TotalIgnored => Projects.Sum(project => project.Ignored.Count);
    public int TotalPackages => TotalLikelyUnused + TotalPossiblyUnused + TotalUsed + TotalIgnored;
}

internal sealed record SourceUsageIndex(
    IReadOnlySet<string> UsingNamespaces,
    IReadOnlySet<string> Namespaces,
    IReadOnlySet<string> AttributeNames,
    IReadOnlySet<string> Tokens);
