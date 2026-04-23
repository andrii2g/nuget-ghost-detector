using System.Text;
using A2G.NuGetGhostDetector.Models;

namespace A2G.NuGetGhostDetector.Reporting;

internal static class MarkdownReporter
{
    public static string Render(ScanResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# NuGet Ghost Detector Report");
        builder.AppendLine();
        builder.AppendLine($"Input: `{Escape(result.InputPath)}`  ");
        builder.AppendLine($"Analysis mode: `{(result.AnalysisMode == AnalysisMode.Full ? "full" : "fallback-no-assets")}`");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Projects scanned | {result.Projects.Count} |");
        builder.AppendLine($"| Direct packages scanned | {result.TotalPackages} |");
        builder.AppendLine($"| Used | {result.TotalUsed} |");
        builder.AppendLine($"| Likely unused | {result.TotalLikelyUnused} |");
        builder.AppendLine($"| Possibly unused | {result.TotalPossiblyUnused} |");
        builder.AppendLine($"| Ignored | {result.TotalIgnored} |");

        foreach (var project in result.Projects)
        {
            builder.AppendLine();
            builder.AppendLine($"## Project: `{Escape(project.Path)}`");
            AppendTable(builder, "Likely unused", project.LikelyUnused);
            AppendTable(builder, "Possibly unused", project.PossiblyUnused);
            AppendTable(builder, "Used", project.Used);
            AppendTable(builder, "Ignored", project.Ignored);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendTable(StringBuilder builder, string title, IReadOnlyList<PackageUsageResult> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"### {title}");
        builder.AppendLine();
        builder.AppendLine("| Package | Version | Reason | Evidence |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var result in results)
        {
            builder.AppendLine($"| {Escape(result.PackageId)} | {Escape(result.Version ?? string.Empty)} | {Escape(result.Reason)} | {Escape(string.Join(", ", result.Evidence))} |");
        }
    }

    private static string Escape(string value)
        => value.Replace("|", "\\|");
}
