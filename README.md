# GrayMint.Common

**Common best practices and utilities for .NET and ASP.NET Core** — async utilities, background jobs, a ready-to-go startup pipeline, EF Core helpers, and OpenAPI/Swagger integration. Battle-tested in production GrayMint services and published as small, focused NuGet packages so you can pull in exactly what you need.

[![NuGet](https://img.shields.io/nuget/v/GrayMint.Common.svg)](https://www.nuget.org/packages/GrayMint.Common)
[![License](https://img.shields.io/github/license/graymint/GrayMint.Common.svg)](LICENSE)

| Package | What it gives you |
| --- | --- |
| [GrayMint.Common](https://www.nuget.org/packages/GrayMint.Common) | Core utilities: recurring jobs, `AsyncLock`, `FastDateTime`, `Patch<T>`, typed API client base, canonical exceptions. No framework dependencies. |
| [GrayMint.Common.AspNetCore](https://www.nuget.org/packages/GrayMint.Common.AspNetCore) | One-call service registration and pipeline setup, JSON exception handling middleware, DI-friendly background jobs, graceful lifetime hooks. |
| [GrayMint.Common.EntityFrameworkCore](https://www.nuget.org/packages/GrayMint.Common.EntityFrameworkCore) | Enum lookup-table sync, safe table creation, provider-aware schema checks (SQL Server & PostgreSQL). |
| [GrayMint.Common.Swagger](https://www.nuget.org/packages/GrayMint.Common.Swagger) | NSwag, OpenAPI, and Scalar UI wired up with bearer-token auth in two calls. |

All packages target `net10.0` and ship under a single shared version.

```sh
dotnet add package GrayMint.Common
```

## Highlights

### Recurring jobs without the ceremony

`Job` runs a delegate on an interval with retry, failure counting, and shared scheduling — no `Timer` bookkeeping, no hosted-service boilerplate. Jobs are multiplexed onto shared `JobRunner` instances with bounded parallelism instead of each spinning its own timer thread.

```csharp
using GrayMint.Common.Jobs;

var job = new Job(async ct => await CleanupExpiredSessions(ct),
    new JobOptions {
        Name = "session-cleanup",
        Interval = TimeSpan.FromMinutes(5),
        MaxRetry = 3
    });
```

In ASP.NET Core, implement `IGrayMintJob` and register it — the job resolves from a DI scope on every run:

```csharp
public class SessionCleanupJob(AppDbContext db) : IGrayMintJob
{
    public async ValueTask RunJob(CancellationToken ct) => /* ... */;
}

builder.Services.AddGrayMintJob<SessionCleanupJob>(
    new GrayMintJobOptions { Name = "session-cleanup", Interval = TimeSpan.FromMinutes(5) });
```

### Async primitives that hold up under load

```csharp
using GrayMint.Common.Utils;

// Keyed async lock: serialize work per resource across the process
using var scope = await AsyncLock.LockAsync($"user/{userId}", ct);

// Or with a timeout instead of waiting forever
using var scope2 = await myLock.LockAsync(TimeSpan.FromSeconds(5), ct);
if (!scope2.Succeeded) return;

// Bounded parallel foreach with cancellation
await GmUtils.ForEachAsync(items, ProcessItemAsync, maxDegreeOfParallelism: 8, ct);
```

`FastDateTime.Now` / `FastDateTime.UtcNow` serve a cached clock with configurable precision (1s by default) — built for hot paths that read the time thousands of times per second, where `DateTime.UtcNow` syscall overhead actually shows up.

### One `Program.cs` for all your services

`GrayMint.Common.AspNetCore` collapses the repetitive startup code every API repeats — CORS, controllers, exception handling, HTTPS redirection — into two calls with opt-in flags:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrayMintCommonServices(new RegisterServicesOptions());
builder.Services.AddGrayMintSwagger(new AddSwaggerOptions { Title = "My API" });

var app = builder.Build();
app.UseGrayMintCommonServices(new UseServicesOptions());
app.UseGrayMintSwagger();

await app.Services.UseGrayMintDatabaseCommand<AppDbContext>(args); // /recreateDb, EnsureCreated
await GrayMintApp.RunAsync(app, args);                             // honors /initOnly
```

### Exceptions that speak HTTP

Throw domain exceptions anywhere; the middleware maps them to the right status code and a consistent JSON `ApiError` body — `NotExistsException` → 404, `AlreadyExistsException` → 409, `UnauthorizedAccessException` → 403, `AuthenticationException` → 401. On the client side, `ApiClientBase` and `ApiException` reverse the mapping, so a typed client (e.g. NSwag-generated) can rethrow the server's original error type.

```csharp
// Server: just throw
throw new NotExistsException($"Project {projectId} was not found.");

// Client: catch it as structured data
catch (ApiException ex) when (ex.Is<NotExistsException>()) { /* 404 with full ApiError */ }
```

### Partial updates with `Patch<T>`

`Patch<T>` distinguishes "field not sent" from "field set to null" in PATCH endpoints — the classic JSON update problem:

```csharp
public class UpdateUserRequest
{
    public Patch<string>? Name { get; init; }        // null → don't touch
    public Patch<string?>? Description { get; init; } // Patch(null) → clear it
}

if (request.Name != null) user.Name = request.Name;
```

Alongside it, `Generics` provides the small contract types every API ends up needing, like `ListResult<T>` (items + total count for paging) and `IdName`.

### EF Core helpers

```csharp
// Keep an enum lookup table in sync with its C# enum (adds, renames, removes)
await EfCoreUtil.UpdateEnums<StatusLookup, Status>(db.StatusLookups);

// Create tables idempotently; works on SQL Server and PostgreSQL
await EfCoreUtil.EnsureTablesCreated(db.Database);

// Provider-aware existence checks
bool hasFn = await EfCoreUtil.SqlFunctionExists(db.Database, "dbo", "fnCalcUsage");

// Named default constraints (SQL Server) that stay portable
builder.Property(x => x.IsEnabled)
    .HasDefaultValueWithConstraintName(true, "DF_Device_IsEnabled");
```

### OpenAPI, NSwag & Scalar in two lines

`AddGrayMintSwagger` / `UseGrayMintSwagger` set up NSwag and/or the .NET OpenAPI generator with the Scalar UI, register a bearer-token security scheme, fix primitive-type schemas, and optionally redirect `/` to the docs UI — so every service exposes consistent, client-generation-ready API docs.

## Development

- `src/` — the packable libraries. Shared version and package metadata come from `src/Directory.Build.props`; each csproj declares only its `Description`, `TargetFramework`, and references.
- `tests/` — unit tests (`GrayMint.Common.Test`) and a sample host (`TestWebApp`). Never packed.
- `pub/` — versioning and update scripts; `pub/PubVersion.json` is the version source of truth for all packages.
- `.github/workflows/` — every push to `main` builds, tests, bumps the version, and publishes to nuget.org; a daily job updates NuGet dependencies and republishes when tests pass. Nothing ships when tests fail.

Before editing files under `src/GrayMint.Common`, see [docs/synced-classes.md](docs/synced-classes.md) — some files are refreshed as a set and should not be modified individually.

See [docs/versioning-and-nuget-updates.md](docs/versioning-and-nuget-updates.md) for how the versioning, publish, and auto-update mechanism works and how to reuse it in another repo.

```sh
dotnet build GrayMint.Common.slnx
dotnet test GrayMint.Common.slnx
```

## License

[MIT](LICENSE)
