# GrayMint.Common

Common utilities shared across GrayMint projects, published to nuget.org:

| Package | Description |
| --- | --- |
| `GrayMint.Common` | General utilities (async helpers, jobs, API client base). |
| `GrayMint.Common.AspNetCore` | Startup skeleton and utilities for ASP.NET Core apps. |
| `GrayMint.Common.EntityFrameworkCore` | Utilities for EF Core projects. |
| `GrayMint.Common.Swagger` | NSwag / OpenAPI utilities. |

## Repo layout

- `src/` — the packable libraries. One shared version and package metadata via
  `src/Directory.Build.props`.
- `tests/` — unit tests (`GrayMint.Common.Test`) and a sample host (`TestWebApp`). Never packed.
- `pub/` — versioning scripts; `pub/PubVersion.json` is the version source of truth.
- `.github/workflows/` — CI: every push to `main` builds, tests, bumps the version, and publishes;
  a daily job updates all NuGet dependencies to the latest versions and republishes when tests pass.

See [docs/versioning-and-nuget-updates.md](docs/versioning-and-nuget-updates.md) for how the
versioning/publish/auto-update mechanism works and how to reuse it in another repo.

## Build

```sh
dotnet build GrayMint.Common.slnx
dotnet test GrayMint.Common.slnx
```
