# GrayMint.Common

**Common best practices and utilities for .NET and ASP.NET Core** — async utilities, background jobs, a ready-to-go startup pipeline, EF Core helpers, and OpenAPI integration. Battle-tested in production GrayMint services and published as small, focused NuGet packages so you can pull in exactly what you need.

[![NuGet](https://img.shields.io/nuget/v/GrayMint.Common.svg)](https://www.nuget.org/packages/GrayMint.Common)
[![License](https://img.shields.io/github/license/graymint/GrayMint.Common.svg)](LICENSE)

| Package | What it gives you |
| --- | --- |
| [GrayMint.Common](https://www.nuget.org/packages/GrayMint.Common) | Core utilities: recurring jobs, `AsyncLock`, `FastDateTime`, `Patch<T>`, typed API client base, canonical exceptions. No framework dependencies. |
| [GrayMint.Common.AspNetCore](https://www.nuget.org/packages/GrayMint.Common.AspNetCore) | One-call service registration and pipeline setup, JSON exception handling middleware, DI-friendly background jobs, graceful lifetime hooks. |
| [GrayMint.Common.EntityFrameworkCore](https://www.nuget.org/packages/GrayMint.Common.EntityFrameworkCore) | Enum lookup-table sync, safe table creation, provider-aware schema checks (SQL Server & PostgreSQL). |
| [GrayMint.Common.Acme](https://www.nuget.org/packages/GrayMint.Common.Acme) | Let's Encrypt certificates for your users' domains over the HTTP-01 challenge: the order with a check of port 80 first, the challenge's answer, and Kestrel's certificate for each domain of a connection. |
| [GrayMint.Common.OpenApi](https://www.nuget.org/packages/GrayMint.Common.OpenApi) | ASP.NET Core's own OpenAPI document, worded from the app's doc comments, and the Scalar UI, with bearer-token auth, in two calls; a client generated from the document keeps NSwag's names and types. Formerly GrayMint.Common.Swagger. |

All packages target `net10.0` and ship under a single shared version; GrayMint.Common.OpenApi targets `net11.0` too, as each .NET has its own OpenAPI generator.

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

In ASP.NET Core, implement `IGrayMintJob` and register it — the job runs as a hosted service on its own schedule, resolves from a DI scope on every run, and the app's stop cancels a run in progress and waits for it:

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

`FastDateTime.UtcNow` serves a cached UTC clock with configurable precision (1s by default) — built for hot paths that read the time thousands of times per second, where `DateTime.UtcNow` syscall overhead actually shows up.

### One `Program.cs` for all your services

`GrayMint.Common.AspNetCore` collapses the repetitive startup code every API repeats — CORS, controllers, exception handling, HTTPS redirection — into two calls with opt-in flags:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrayMintCommonServices(new RegisterServicesOptions());
builder.Services.AddGrayMintOpenApi(new AddOpenApiOptions { Title = "My API" });

var app = builder.Build();
app.UseGrayMintCommonServices(new UseServicesOptions());
app.UseGrayMintOpenApi();

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
```

Named default constraints come from EF Core itself: `modelBuilder.Model.UseNamedDefaultConstraints(true)` names
each default `DF_<table>_<column>` where the database names one (SQL Server), on any provider.

### Let's Encrypt certificates for your users' domains

`AcmeCertificateIssuer` gets a certificate for one domain, from the order to the signed certificate. Where the challenge is kept, and the certificate, is yours: here, the database, which whatever answers port 80 reads.

```csharp
using GrayMint.Common.Acme;

var issuer = new AcmeCertificateIssuer(new AcmeClient()); // Let's Encrypt; AcmeDirectories.LetsEncryptStaging to try it out
var accountKeyPem = await issuer.CreateAccount(); // once, kept for every later order

byte[] pfx = await issuer.Issue(new AcmeIssueRequest {
    AccountKeyPem = accountKeyPem,
    Domain = "checkout.example.com",
    PublishChallenge = (challenge, ct) => SaveChallenge(challenge, ct),
    WithdrawChallenge = (challenge, ct) => DeleteChallenge(challenge, ct),
    // our own read of http://domain/.well-known/acme-challenge/token first: a domain that does not point to the
    // server yet fails here, not at Let's Encrypt, which allows five failures an hour for a domain
    SelfCheckHandlerFactory = () => CreatePublicOnlyHandler()
});
```

The PFX has the key, the certificate and Let's Encrypt's intermediates, with no password. The domain is your user's, so the self-check's handler should connect to public addresses only and follow no redirect. A failed check throws `AcmeSelfCheckException`, whose `Error` says what to fix: no address, port 80 not reached, or another server's answer.

The server that answers port 80, and serves each domain over HTTPS with its own certificate:

```csharp
var selector = new TlsCertificateSelector(LoadPfx, new TlsCertificateSelectorOptions { DefaultDomain = "checkout.example.com" });
builder.WebHost.ConfigureKestrel(kestrel => {
    kestrel.ListenAnyIP(80);
    kestrel.ListenAnyIP(443, listen => listen.UseHttps(selector));
});

var app = builder.Build();
app.UseAcmeChallenge((domain, token, ct) => FindKeyAuthorization(domain, token, ct)); // first: the challenge is read on port 80
```

`LoadPfx` runs for every name a client sends that is not in the selector's cache, so refuse a name you do not serve without a database read. A loaded certificate is used for 10 minutes (`CacheTimeout`), which takes a renewed one; `Invalidate(domain)` takes it at once. A domain no connection names for an hour (`IdleTimeout`) leaves the cache, and a load that fails keeps the certificate loaded before. `AcmeClientOptions.HttpClient` sends the authority's requests through a client of yours, such as one through a proxy.

To have the certificates kept, renewed and served for you, in tables of their own on PostgreSQL or SQL Server, use [GrayMint.TlsCertificates](https://github.com/graymint/GrayMint.TlsCertificates), which is built on this package.

### OpenAPI & Scalar in two lines

`AddGrayMintOpenApi` / `UseGrayMintOpenApi` set up ASP.NET Core's own OpenAPI document at `/openapi/v1.json` with the Scalar UI, register a bearer-token security scheme, fix primitive-type schemas, and optionally redirect `/` to the docs UI. The Scalar page loads nothing from Scalar's servers: the service itself serves Scalar's fonts, Inter and JetBrains Mono (SIL Open Font License), at `/scalar/fonts/`, and Scalar's telemetry is off.

The document says what NSwag's own document said, so a client generated from it keeps NSwag's names and types: a `{Controller}Client` class per controller with an `{Action}Async` method per action and its parameters in NSwag's order, a body parameter by its C# name, a generic type by NSwag's name (`PatchOfNullableInteger`) and a second type of one name numbered (`User2`), a class with its base class, a `decimal` as a decimal, a form's file as an optional file, a file for any `IActionResult`, and a property required when it is never null or declared `required`. Where ASP.NET Core's generator leaves a schema empty (a type it first meets in a collection, with a property back to a type above it) or adds null to an enum's, the document has the schema NSwag's had. Only the schemas' order differs: Microsoft.OpenApi writes them by name, so a client's classes come in that order.

#### Its words come from the doc comments

The document takes its words from the app's doc comments and its packages': an action's summary, remarks, parameters, body and `<returns>`; a type's and a property's summary, and an enum's values with theirs; and a section's, a controller's summary and remarks. ASP.NET Core's own reader of them works only for an `AddOpenApi` call in the app's project, which this package makes for it. An action with `[AllowAnonymous]` asks for no token.

```csharp
builder.Services.AddGrayMintOpenApi(new AddOpenApiOptions {
    Title = "My API",
    Description = "The introduction, in Markdown.",
    Version = typeof(Program).Assembly.GetName().Version?.ToString(3)
});
```

`[ApiSection("Orders", 10)]` on a controller gives its section the name a reader sees and its place: sections with a place come first, by it. The tag keeps the controller's name, which the clients are named by. The app ships its comments, and copies its packages' beside it:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<CopyDocumentationFilesFromPackages>true</CopyDocumentationFilesFromPackages>
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

A word of the app's own, such as who may call an action, comes from an operation transformer it adds to the `"v1"` document after `AddGrayMintOpenApi`, which runs after this package's.

### Clients with no running service

Microsoft's build-time generator writes the document to a file when the API project builds:

```xml
<!-- the API project -->
<PackageReference Include="Microsoft.Extensions.ApiDescription.Server" Version="11.0.0" PrivateAssets="all" />
<PropertyGroup>
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/../MyApi.Client</OpenApiDocumentsDirectory>
</PropertyGroup>
```

It runs the app's startup with a stand-in server to read the endpoints, so skip what needs a real one, such as a database, while `Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider"`. The tool of the app's own .NET loads that .NET's OpenAPI generator, which is why this package targets both .NET 10 and .NET 11.

To write the file only when you ask, set `<OpenApiGenerateDocumentsOnBuild>false</OpenApiGenerateDocumentsOnBuild>` and build with `-p:OpenApiGenerateDocumentsOnBuild=true`.

#### With NSwag's CLI, as before

An `.nswag` file that made the clients from the running app keeps its code generators. Only its document generator changes, from `aspNetCoreToOpenApi` to `fromDocument` on the written file, and the clients come out with the same classes, methods and types:

```json
{
  "runtime": "Net100",
  "documentGenerator": {
    "fromDocument": { "url": "MyApi.json" }
  },
  "codeGenerators": {
    "openApiToCSharpClient": {
      "className": "{controller}Client",
      "operationGenerationMode": "MultipleClientsFromOperationId",
      "jsonLibrary": "SystemTextJson",
      "namespace": "MyApi.Client",
      "output": "MyApi.Client.cs"
    },
    "openApiToTypeScriptClient": {
      "className": "{controller}Client",
      "operationGenerationMode": "MultipleClientsFromOperationId",
      "output": "MyApi.Client.ts"
    }
  }
}
```

```shell
dotnet build MyApi -p:OpenApiGenerateDocumentsOnBuild=true
dotnet tool run nswag run MyApi.nswag
```

`{controller}Client` with `MultipleClientsFromOperationId` gives one client per controller, as the document's operation ids are `{Controller}_{Action}`. Your other settings stay as they are. Two things can differ from before: a body is nullable where the action declares it so, and a derived class carries its base class's properties itself, as the document has no inheritance for a type that is not polymorphic.

#### With the client project's build

A client project names the file, and its build generates the client:

```xml
<!-- the client project -->
<PackageReference Include="NSwag.ApiDescription.Client" Version="14.8.0" PrivateAssets="all" />
<OpenApiReference Include="MyApi.json" CodeGenerator="NSwagCSharp" Namespace="MyApi.Client"
                  ClassName="{controller}Client" Options="/JsonLibrary:SystemTextJson" />
```

`CodeGenerator="NSwagTypeScript"` makes a TypeScript client the same way.

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
