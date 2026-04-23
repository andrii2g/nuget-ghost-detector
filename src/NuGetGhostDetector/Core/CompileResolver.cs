using NuGetGhostDetector.Infrastructure;

namespace NuGetGhostDetector.Core;

internal sealed class CompileResolver
{
    public IReadOnlyList<string> Resolve(PackageResolutionResult packageContext)
    {
        var projectDirectory = Path.GetDirectoryName(packageContext.CompileInputs.ProjectPath)!;
        var includedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (packageContext.CompileInputs.EnableDefaultCompileItems)
        {
            foreach (var file in FileSystemHelpers.EnumerateFilesRecursive(projectDirectory, file =>
                         file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                         !FileSystemHelpers.IsGeneratedSourceFile(file)))
            {
                includedFiles.Add(file);
            }
        }

        foreach (var include in packageContext.CompileInputs.Includes)
        {
            foreach (var file in FileSystemHelpers.ExpandGlob(include.DeclaringDirectory, include.Include))
            {
                if (!FileSystemHelpers.IsGeneratedSourceFile(file))
                {
                    includedFiles.Add(file);
                }
            }
        }

        foreach (var remove in packageContext.CompileInputs.Removes)
        {
            foreach (var file in FileSystemHelpers.ExpandGlob(remove.DeclaringDirectory, remove.Remove))
            {
                includedFiles.Remove(file);
            }
        }

        return includedFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
