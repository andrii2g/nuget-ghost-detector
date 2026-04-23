using NuGetGhostDetector.Cli;
using NuGetGhostDetector.Core;
using NuGetGhostDetector.Reporting;

var parseResult = CliParser.Parse(args);

if (!parseResult.Success)
{
    Console.Error.WriteLine(parseResult.ErrorMessage);
    Environment.ExitCode = 1;
    return;
}

var options = parseResult.Options!;
if (options.ShowHelp)
{
    Console.WriteLine(CliParser.GetHelpText());
    return;
}

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

    Environment.ExitCode = shouldFail ? 2 : 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
}
