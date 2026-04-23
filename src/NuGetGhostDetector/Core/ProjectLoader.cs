using System.Text.RegularExpressions;
using System.Xml.Linq;
using NuGetGhostDetector.Infrastructure;

namespace NuGetGhostDetector.Core;

internal sealed record ProjectLoadResult(string RootPath, IReadOnlyList<string> ProjectPaths, IReadOnlyList<string> Warnings);

internal sealed class ProjectLoader
{
    private static readonly Regex SolutionProjectRegex = new(
        "Project\\(\"\\{[^\\}]+\\}\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ProjectLoadResult DiscoverProjects(string inputPath)
    {
        var warnings = new List<string>();
        var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string rootPath;

        if (Directory.Exists(inputPath))
        {
            rootPath = inputPath;
            foreach (var project in FileSystemHelpers.EnumerateFilesRecursive(inputPath, file => file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
            {
                projects.Add(project);
            }
        }
        else if (inputPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            rootPath = Path.GetDirectoryName(inputPath)!;
            var solutionText = File.ReadAllText(inputPath);
            foreach (Match match in SolutionProjectRegex.Matches(solutionText))
            {
                var relativePath = match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
                var projectPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
                if (File.Exists(projectPath))
                {
                    projects.Add(projectPath);
                }
                else
                {
                    warnings.Add($"Warning: solution referenced missing project: {projectPath}");
                }
            }
        }
        else if (inputPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            rootPath = Path.GetDirectoryName(inputPath)!;
            var document = XDocument.Load(inputPath);
            foreach (var projectElement in document.Descendants().Where(element => element.Name.LocalName == "Project"))
            {
                var relativePath = projectElement.Attribute("Path")?.Value;
                if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var projectPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
                if (File.Exists(projectPath))
                {
                    projects.Add(projectPath);
                }
                else
                {
                    warnings.Add($"Warning: solution referenced missing project: {projectPath}");
                }
            }
        }
        else if (inputPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            rootPath = Path.GetDirectoryName(inputPath)!;
            projects.Add(Path.GetFullPath(inputPath));
        }
        else
        {
            throw new InvalidOperationException($"unsupported input path: {inputPath}");
        }

        return new ProjectLoadResult(rootPath, projects.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray(), warnings);
    }
}
