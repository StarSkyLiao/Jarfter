# Jarfter

Jarfter is a lightweight C# solution repository for versioned Jarfter modules.

The repository is intentionally thin:

- The root solution links all modules together for local development.
- Each module is a standalone C# project that can be packed as a NuGet package.
- `Jarfter.Core` contains shared contracts used by future extension modules.
- Concrete extension modules are not generated until there is a real feature boundary.

## Repository Layout

```text
Jarfter/
  Jarfter.sln
  Directory.Build.props
  Directory.Packages.props
  src/
    Jarfter.Core/
      Jarfter.Core.csproj
```

Future modules should follow the same shape:

```text
src/
  Jarfter.Extensions.SomeFeature/
    Jarfter.Extensions.SomeFeature.csproj
```

Extension modules should reference `Jarfter.Core` during local development and depend on the published `Jarfter.Core` NuGet package when consumed by external projects.

## Branch Strategy

Recommended branch usage:

- `master`: stable release branch; only publish formal package versions from here.
- `dev`: integration branch for active development and prerelease packages.
- `feature/*`: short-lived feature branches.

Recommended versioning:

- Stable package: `0.1.0`, `0.2.0`, `1.0.0`.
- Prerelease package: `0.2.0-dev.1`, `0.2.0-dev.2`.

## Worktree Strategy

Use Git worktrees at the repository level, not per module.

Example after the first commit and after creating the `dev` branch:

```powershell
git worktree add ..\Jarfter-master master
git worktree add ..\Jarfter-dev dev
```

This keeps stable and development branches available side by side without coupling local workspace layout to module structure.

## Build

```powershell
dotnet restore
dotnet build Jarfter.sln
```

## Pack

```powershell
dotnet pack src\Jarfter.Core\Jarfter.Core.csproj -c Release -o artifacts\packages
```

