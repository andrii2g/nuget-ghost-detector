using System.Text;

namespace NuGetGhostDetector.Cli;

internal static class CliParser
{
    public static CliParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return HelpResult();
        }

        if (HasHelp(args))
        {
            return HelpResult();
        }

        if (!string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
        {
            return new CliParseResult(false, null, $"Error: unsupported command '{args[0]}'. Expected 'scan'.");
        }

        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith("--", StringComparison.Ordinal))
        {
            return new CliParseResult(false, null, "Error: missing required <path> argument.");
        }

        var inputPath = args[1];
        var format = OutputFormat.Console;
        string? outputPath = null;
        var includePossible = false;
        var failOnGhosts = false;
        var verbose = false;
        var ignoredPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 2; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--format":
                    if (i + 1 >= args.Length)
                    {
                        return new CliParseResult(false, null, "Error: --format requires a value.");
                    }

                    i++;
                    if (!TryParseFormat(args[i], out format))
                    {
                        return new CliParseResult(false, null, $"Error: unsupported format '{args[i]}'.");
                    }
                    break;

                case "--output":
                    if (i + 1 >= args.Length)
                    {
                        return new CliParseResult(false, null, "Error: --output requires a value.");
                    }

                    outputPath = args[++i];
                    break;

                case "--include-possible":
                    includePossible = true;
                    break;

                case "--fail-on-ghosts":
                    failOnGhosts = true;
                    break;

                case "--verbose":
                    verbose = true;
                    break;

                case "--ignore":
                    if (i + 1 >= args.Length)
                    {
                        return new CliParseResult(false, null, "Error: --ignore requires a value.");
                    }

                    foreach (var package in args[++i].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        ignoredPackages.Add(package);
                    }
                    break;

                default:
                    return new CliParseResult(false, null, $"Error: unknown option '{arg}'.");
            }
        }

        return new CliParseResult(true, new CliOptions(
            Command: "scan",
            InputPath: inputPath,
            Format: format,
            OutputPath: outputPath,
            IncludePossible: includePossible,
            FailOnGhosts: failOnGhosts,
            Verbose: verbose,
            ShowHelp: false,
            IgnoredPackages: ignoredPackages), null);
    }

    public static string GetHelpText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("NuGet Ghost Detector");
        builder.AppendLine();
        builder.AppendLine("Usage:");
        builder.AppendLine("  nuget-ghost scan <path> [options]");
        builder.AppendLine();
        builder.AppendLine("Options:");
        builder.AppendLine("  --format <console|markdown|json>   Default: console");
        builder.AppendLine("  --output <path>                    Write report to a file");
        builder.AppendLine("  --ignore <packageId[,packageId]>   Repeatable, case-insensitive");
        builder.AppendLine("  --fail-on-ghosts                   Exit 2 on ghost findings");
        builder.AppendLine("  --include-possible                 Count PossiblyUnused in failure predicate");
        builder.AppendLine("  --verbose                          Print discovery details");
        builder.AppendLine("  --help                             Show this help text");
        return builder.ToString().TrimEnd();
    }

    private static bool HasHelp(IEnumerable<string> args)
        => args.Any(arg => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase));

    private static bool TryParseFormat(string rawValue, out OutputFormat format)
    {
        switch (rawValue.ToLowerInvariant())
        {
            case "console":
                format = OutputFormat.Console;
                return true;
            case "markdown":
                format = OutputFormat.Markdown;
                return true;
            case "json":
                format = OutputFormat.Json;
                return true;
            default:
                format = default;
                return false;
        }
    }

    private static CliParseResult HelpResult()
        => new(true, new CliOptions(
            Command: "help",
            InputPath: string.Empty,
            Format: OutputFormat.Console,
            OutputPath: null,
            IncludePossible: false,
            FailOnGhosts: false,
            Verbose: false,
            ShowHelp: true,
            IgnoredPackages: new HashSet<string>(StringComparer.OrdinalIgnoreCase)), null);
}
