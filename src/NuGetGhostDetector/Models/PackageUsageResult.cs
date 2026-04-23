namespace A2G.NuGetGhostDetector.Models;

internal sealed record PackageUsageResult(
    string PackageId,
    string? Version,
    PackageClassification Classification,
    string Reason,
    IReadOnlyList<string> Evidence);
