using NuGetGhostDetector.Models;

namespace NuGetGhostDetector.Reporting;

internal static class ConsoleReporter
{
    public static string Render(ScanResult result)
    {
        var lines = new List<string>
        {
            "NuGet Ghost Detector",
            $"Input: {result.InputPath}",
            $"Analysis mode: {(result.AnalysisMode == AnalysisMode.Full ? "full" : "fallback-no-assets")}",
            $"Projects scanned: {result.Projects.Count}",
            $"Direct packages scanned: {result.TotalPackages}",
            string.Empty
        };

        foreach (var project in result.Projects)
        {
            lines.Add($"Project: {project.Path}");
            AppendSection(lines, "Likely unused", project.LikelyUnused);
            AppendSection(lines, "Possibly unused", project.PossiblyUnused);
            AppendSection(lines, "Used", project.Used);
            AppendSection(lines, "Ignored", project.Ignored);
            lines.Add(string.Empty);
        }

        lines.Add("Summary:");
        lines.Add($"  Used: {result.TotalUsed}");
        lines.Add($"  Likely unused: {result.TotalLikelyUnused}");
        lines.Add($"  Possibly unused: {result.TotalPossiblyUnused}");
        lines.Add($"  Ignored: {result.TotalIgnored}");

        return string.Join(Environment.NewLine, lines).TrimEnd();
    }

    private static void AppendSection(List<string> lines, string title, IReadOnlyList<PackageUsageResult> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        lines.Add($"{title}:");
        foreach (var result in results)
        {
            var version = string.IsNullOrWhiteSpace(result.Version) ? string.Empty : $" {result.Version}";
            lines.Add($"  - {result.PackageId}{version}");
            lines.Add($"    Reason: {result.Reason}");
            if (result.Evidence.Count > 0)
            {
                lines.Add($"    Evidence: {string.Join(", ", result.Evidence)}");
            }
        }
    }
}
