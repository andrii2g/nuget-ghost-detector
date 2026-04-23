namespace NuGetGhostDetector.Cli;

internal enum OutputFormat
{
    Console,
    Markdown,
    Json
}

internal sealed record CliOptions(
    string Command,
    string InputPath,
    OutputFormat Format,
    string? OutputPath,
    bool IncludePossible,
    bool FailOnGhosts,
    bool Verbose,
    bool ShowHelp,
    IReadOnlySet<string> IgnoredPackages);

internal sealed record CliParseResult(bool Success, CliOptions? Options, string? ErrorMessage);
