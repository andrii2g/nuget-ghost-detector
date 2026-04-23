namespace A2G.NuGetGhostDetector.Cli;

internal enum OutputFormat
{
    Console,
    Markdown,
    Json
}

internal sealed record CliOptions(
    string InputPath,
    OutputFormat Format,
    string? OutputPath,
    bool IncludePossible,
    bool FailOnGhosts,
    bool Verbose,
    IReadOnlySet<string> IgnoredPackages);
