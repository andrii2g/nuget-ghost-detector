using A2G.NuGetGhostDetector.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace A2G.NuGetGhostDetector.Core;

internal sealed class CodeScanner
{
    private static readonly Regex UsingRegex = new(
        @"using\s+(?:static\s+)?([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;",
        RegexOptions.Compiled);

    private static readonly Regex NamespaceRegex = new(
        @"namespace\s+([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)",
        RegexOptions.Compiled);

    private static readonly Regex AttributeRegex = new(
        @"\[([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    private static readonly Regex TokenRegex = new(
        @"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*",
        RegexOptions.Compiled);

    public SourceUsageIndex Scan(IReadOnlyList<string> sourceFiles, bool verbose)
    {
        var usingNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                var text = ReadText(sourceFile);
                foreach (Match match in UsingRegex.Matches(text))
                {
                    usingNamespaces.Add(match.Groups[1].Value);
                }

                foreach (Match match in NamespaceRegex.Matches(text))
                {
                    namespaces.Add(match.Groups[1].Value);
                }

                foreach (Match match in AttributeRegex.Matches(text))
                {
                    attributes.Add(match.Groups[1].Value);
                }

                foreach (Match match in TokenRegex.Matches(text))
                {
                    tokens.Add(match.Value);
                }
            }
            catch when (!verbose)
            {
            }
        }

        return new SourceUsageIndex(usingNamespaces, namespaces, attributes, tokens);
    }

    private static string ReadText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
    }
}
