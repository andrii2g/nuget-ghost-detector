using System.CommandLine;
using A2G.NuGetGhostDetector.Core;
using A2G.NuGetGhostDetector.Reporting;

namespace A2G.NuGetGhostDetector.Cli;

internal static class CliParser
{
    public static Task<int> InvokeAsync(string[] args)
    {
        var pathArgument = new Argument<string>("path")
        {
            Description = "Solution, project, or directory to scan."
        };

        var formatOption = new Option<OutputFormat>("--format", [])
        {
            Description = "Output format: console, markdown, or json.",
            DefaultValueFactory = _ => OutputFormat.Console
        };

        var outputOption = new Option<string?>("--output", [])
        {
            Description = "Write the rendered report to a file."
        };

        var includePossibleOption = new Option<bool>("--include-possible", [])
        {
            Description = "Count PossiblyUnused packages in the failure predicate."
        };

        var failOnGhostsOption = new Option<bool>("--fail-on-ghosts", [])
        {
            Description = "Exit with code 2 when ghost findings match the configured predicate."
        };

        var verboseOption = new Option<bool>("--verbose", [])
        {
            Description = "Print discovery warnings and parsing details."
        };

        var ignoreOption = new Option<string[]>("--ignore", [])
        {
            Description = "Ignore package IDs. Repeatable or comma-separated.",
            AllowMultipleArgumentsPerToken = true
        };
        ignoreOption.Arity = ArgumentArity.ZeroOrMore;

        var scanCommand = new Command("scan", "Scan a solution, project, or directory for likely unused NuGet packages.");
        scanCommand.Arguments.Add(pathArgument);
        scanCommand.Options.Add(formatOption);
        scanCommand.Options.Add(outputOption);
        scanCommand.Options.Add(includePossibleOption);
        scanCommand.Options.Add(failOnGhostsOption);
        scanCommand.Options.Add(verboseOption);
        scanCommand.Options.Add(ignoreOption);

        scanCommand.SetAction(parseResult =>
        {
            var options = new CliOptions(
                InputPath: parseResult.GetRequiredValue(pathArgument),
                Format: parseResult.GetValue(formatOption),
                OutputPath: parseResult.GetValue(outputOption),
                IncludePossible: parseResult.GetValue(includePossibleOption),
                FailOnGhosts: parseResult.GetValue(failOnGhostsOption),
                Verbose: parseResult.GetValue(verboseOption),
                IgnoredPackages: (parseResult.GetValue(ignoreOption) ?? [])
                    .SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

            return RunScan(options);
        });

        var rootCommand = new RootCommand("NuGet Ghost Detector");
        rootCommand.Subcommands.Add(scanCommand);
        return rootCommand.Parse(args).InvokeAsync();
    }

    private static int RunScan(CliOptions options)
    {
        try
        {
            var app = new GhostDetectorApp();
            var result = app.Run(options, Directory.GetCurrentDirectory());

            var rendered = options.Format switch
            {
                OutputFormat.Console => ConsoleReporter.Render(result),
                OutputFormat.Markdown => MarkdownReporter.Render(result),
                OutputFormat.Json => JsonReporter.Render(result),
                _ => throw new InvalidOperationException($"Unsupported format: {options.Format}")
            };

            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                Console.WriteLine(rendered);
            }
            else
            {
                var outputPath = Path.GetFullPath(options.OutputPath, Directory.GetCurrentDirectory());
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath, rendered);
                if (options.Format == OutputFormat.Console)
                {
                    Console.WriteLine(rendered);
                }
                else
                {
                    Console.WriteLine($"Wrote {options.Format.ToString().ToLowerInvariant()} report to {outputPath}");
                }
            }

            var shouldFail = options.FailOnGhosts &&
                (result.TotalLikelyUnused > 0 || (options.IncludePossible && result.TotalPossiblyUnused > 0));

            return shouldFail ? 2 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
