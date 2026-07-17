# GrayMint.Common

Common utilities shared across GrayMint projects, published to [nuget.org](https://www.nuget.org/packages?q=GrayMint.Common).

| Package | Description |
| --- | --- |
| [GrayMint.Common](https://www.nuget.org/packages/GrayMint.Common) | General utilities (async helpers, jobs, API client base). |
| [GrayMint.Common.AspNetCore](https://www.nuget.org/packages/GrayMint.Common.AspNetCore) | Startup skeleton and utilities for ASP.NET Core apps. |
| [GrayMint.Common.EntityFrameworkCore](https://www.nuget.org/packages/GrayMint.Common.EntityFrameworkCore) | Utilities for EF Core projects. |
| [GrayMint.Common.Swagger](https://www.nuget.org/packages/GrayMint.Common.Swagger) | NSwag / OpenAPI utilities. |

All packages share a single version, sourced from `pub/PubVersion.json`.

## Repo layout

- `src/` — the packable libraries. One shared version and package metadata via
  `src/Directory.Build.props`; a csproj declares only its `Description`, `TargetFramework`,
  and references.
- `tests/` — unit tests (`GrayMint.Common.Test`) and a sample host (`TestWebApp`). Never packed.
- `pub/` — versioning and update scripts (`Invoke-VersionBump.ps1`,
  `lib/Update-NugetPackages.ps1`); `pub/PubVersion.json` is the version source of truth.
- `.github/workflows/` — CI: every push to `main` builds, tests, bumps the version, and publishes
  to nuget.org; a daily job updates all NuGet dependencies to the latest versions and republishes
  when tests pass. Failures notify by email; nothing ships when tests fail.

See [docs/versioning-and-nuget-updates.md](docs/versioning-and-nuget-updates.md) for how the
versioning/publish/auto-update mechanism works and how to reuse it in another repo.

## Build

```sh
dotnet build GrayMint.Common.slnx
dotnet test GrayMint.Common.slnx
```
