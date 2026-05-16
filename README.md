# NuGet Ghost Detector

NuGet Ghost Detector is a .NET CLI tool that scans SDK-style projects and reports direct `PackageReference` entries that look statically unused.

It is intentionally conservative. Results are heuristics, not proof. Review findings before removing dependencies.

## Usage

```bash
dotnet run --project src/NuGetGhostDetector -- scan <path>
```

Examples:

```bash
dotnet run --project src/NuGetGhostDetector -- scan MySolution.sln
dotnet run --project src/NuGetGhostDetector -- scan src/MyApp/MyApp.csproj
dotnet run --project src/NuGetGhostDetector -- scan . --format markdown --output ghost-report.md
dotnet run --project src/NuGetGhostDetector -- scan . --fail-on-ghosts --include-possible
```

## Input types

The scanner accepts:
- a `.sln` or `.slnx`
- a single `.csproj`
- a directory

## Output formats

Supported formats:
- `console`
- `markdown`
- `json`

Useful options:

```bash
dotnet run --project src/NuGetGhostDetector -- scan . --format json
dotnet run --project src/NuGetGhostDetector -- scan . --ignore Newtonsoft.Json --ignore Serilog
dotnet run --project src/NuGetGhostDetector -- scan . --fail-on-ghosts
```

## Notes

- Direct package discovery supports `.csproj`, `Directory.Build.props`, `Directory.Build.targets`, and simple literal imports.
- Full MSBuild evaluation is not implemented.
- Transitive dependencies are used only as hints and are never reported as direct removal candidates.
- When `obj/project.assets.json` is missing, the tool falls back to package-name heuristics and does not emit `LikelyUnused`.
- The tool never edits project files.
