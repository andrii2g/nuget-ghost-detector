namespace A2G.NuGetGhostDetector.Models;

internal enum PackageClassification
{
    Used = 0,
    LikelyUnused = 1,
    PossiblyUnused = 2,
    Ignored = 3
}
