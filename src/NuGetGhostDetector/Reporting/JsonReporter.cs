using A2G.NuGetGhostDetector.Models;
using System.Text.Json;

namespace A2G.NuGetGhostDetector.Reporting;

internal static class JsonReporter
{
    public static string Render(ScanResult result)
    {
        var payload = new
        {
            analysisMode = result.AnalysisMode == AnalysisMode.Full ? "full" : "fallback-no-assets",
            projects = result.Projects.Select(project => new
            {
                name = project.Name,
                path = project.Path,
                likelyUnused = project.LikelyUnused.Select(ToJsonItem).ToArray(),
                possiblyUnused = project.PossiblyUnused.Select(ToJsonItem).ToArray(),
                used = project.Used.Select(ToJsonItem).ToArray(),
                ignored = project.Ignored.Select(ToJsonItem).ToArray()
            }).ToArray()
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static object ToJsonItem(PackageUsageResult result) => new
    {
        packageId = result.PackageId,
        version = result.Version ?? string.Empty,
        reason = result.Reason,
        evidence = result.Evidence.ToArray()
    };
}
