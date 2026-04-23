using System.Text.RegularExpressions;

namespace NuGetGhostDetector.Infrastructure;

internal static class FileSystemHelpers
{
    private static readonly string[] ExcludedDirectories =
    [
        "bin",
        "obj",
        ".git",
        ".vs",
        ".vscode",
        "packages",
        "node_modules",
        "artifacts",
        "out",
        "TestResults"
    ];

    private static readonly string[] GeneratedSuffixes =
    [
        ".g.cs",
        ".generated.cs",
        ".designer.cs",
        ".AssemblyInfo.cs"
    ];

    public static bool IsExcludedDirectory(string directoryName)
        => ExcludedDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase);

    public static bool IsGeneratedSourceFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (GeneratedSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var normalized = NormalizePath(filePath);
        return normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<string> EnumerateFilesRecursive(string rootPath, Func<string, bool> filePredicate)
    {
        if (!Directory.Exists(rootPath))
        {
            yield break;
        }

        var directories = new Stack<string>();
        directories.Push(rootPath);

        while (directories.Count > 0)
        {
            var current = directories.Pop();

            IEnumerable<string> childDirectories;
            IEnumerable<string> files;
            try
            {
                childDirectories = Directory.EnumerateDirectories(current);
                files = Directory.EnumerateFiles(current);
            }
            catch
            {
                continue;
            }

            foreach (var directory in childDirectories.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsExcludedDirectory(Path.GetFileName(directory)))
                {
                    directories.Push(directory);
                }
            }

            foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (filePredicate(file))
                {
                    yield return Path.GetFullPath(file);
                }
            }
        }
    }

    public static string NormalizePath(string path)
        => Path.GetFullPath(path).Replace('\\', '/');

    public static bool IsSimpleLiteralPath(string rawPath)
        => !string.IsNullOrWhiteSpace(rawPath) && !rawPath.Contains('$') && !rawPath.Contains('%');

    public static IEnumerable<string> ExpandGlob(string declaringDirectory, string rawPattern)
    {
        var pattern = rawPattern.Replace('\\', '/');
        var absolutePattern = Path.IsPathRooted(pattern)
            ? Path.GetFullPath(pattern)
            : Path.GetFullPath(Path.Combine(declaringDirectory, pattern));

        if (!absolutePattern.Contains('*') && !absolutePattern.Contains('?'))
        {
            if (File.Exists(absolutePattern) && absolutePattern.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                yield return absolutePattern;
            }

            yield break;
        }

        var normalized = absolutePattern.Replace('\\', '/');
        var rootPath = GetGlobBasePath(normalized);
        if (!Directory.Exists(rootPath))
        {
            yield break;
        }

        var relativePattern = normalized[rootPath.Length..].TrimStart('/');
        var regex = GlobToRegex(relativePattern);

        foreach (var file in Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
            if (regex.IsMatch(relative))
            {
                yield return Path.GetFullPath(file);
            }
        }
    }

    private static string GetGlobBasePath(string normalizedPattern)
    {
        var wildcardIndex = normalizedPattern.IndexOfAny(['*', '?']);
        if (wildcardIndex < 0)
        {
            return Path.GetDirectoryName(normalizedPattern) ?? normalizedPattern;
        }

        var lastSeparatorIndex = normalizedPattern.LastIndexOf('/', wildcardIndex);
        if (lastSeparatorIndex < 0)
        {
            return Path.GetPathRoot(normalizedPattern) ?? normalizedPattern;
        }

        return normalizedPattern[..lastSeparatorIndex];
    }

    private static Regex GlobToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", @"[^/\\]*")
            .Replace(@"\?", ".");

        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
