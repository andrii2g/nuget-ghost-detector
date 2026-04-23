namespace NuGetGhostDetector.Models;

internal enum PackageSourceKind
{
    ProjectFile = 0,
    DirectoryBuild = 1,
    ImportedFile = 2
}

internal sealed record ProjectPackageReference(
    string ProjectPath,
    string PackageId,
    string? Version,
    string DeclaredInPath,
    PackageSourceKind SourceKind);
