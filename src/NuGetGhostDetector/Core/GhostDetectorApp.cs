using NuGetGhostDetector.Cli;
using NuGetGhostDetector.Models;

namespace NuGetGhostDetector.Core;

internal sealed class GhostDetectorApp
{
    private readonly ProjectLoader _projectLoader = new();
    private readonly PackageResolver _packageResolver = new();
    private readonly CompileResolver _compileResolver = new();
    private readonly CodeScanner _codeScanner = new();
    private readonly AssetsReader _assetsReader = new();
    private readonly Analyzer _analyzer = new();

    public ScanResult Run(CliOptions options, string currentDirectory)
    {
        var inputPath = Path.GetFullPath(options.InputPath, currentDirectory);
        if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
        {
            throw new InvalidOperationException($"input path does not exist: {inputPath}");
        }

        var loadResult = _projectLoader.DiscoverProjects(inputPath);
        if (loadResult.ProjectPaths.Count == 0)
        {
            throw new InvalidOperationException($"no .csproj files found for input: {inputPath}");
        }

        var warnings = new List<string>(loadResult.Warnings);
        var projectResults = new List<ProjectScanResult>();

        foreach (var projectPath in loadResult.ProjectPaths)
        {
            var packageContext = _packageResolver.Resolve(projectPath, loadResult.RootPath, options.Verbose);
            var sourceFiles = _compileResolver.Resolve(packageContext);
            var usageIndex = _codeScanner.Scan(sourceFiles, options.Verbose);
            var assetsInfo = _assetsReader.Read(projectPath, packageContext.DirectPackages, options.Verbose);
            var analysis = _analyzer.Analyze(packageContext.DirectPackages, usageIndex, assetsInfo, options.IgnoredPackages);

            var projectWarnings = packageContext.Warnings.Concat(assetsInfo.Warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            warnings.AddRange(projectWarnings);

            projectResults.Add(new ProjectScanResult(
                Name: Path.GetFileNameWithoutExtension(projectPath),
                Path: projectPath,
                AnalysisMode: analysis.AnalysisMode,
                LikelyUnused: analysis.LikelyUnused,
                PossiblyUnused: analysis.PossiblyUnused,
                Used: analysis.Used,
                Ignored: analysis.Ignored,
                Warnings: projectWarnings));
        }

        var orderedProjects = projectResults.OrderBy(project => project.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        var analysisMode = orderedProjects.Any(project => project.AnalysisMode == AnalysisMode.FallbackNoAssets)
            ? AnalysisMode.FallbackNoAssets
            : AnalysisMode.Full;

        return new ScanResult(inputPath, analysisMode, orderedProjects, warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
