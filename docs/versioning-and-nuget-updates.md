# Versioning, Publishing & Automatic NuGet Updates

How this repo versions, publishes, and keeps its NuGet dependencies up to date — and how to copy
the same mechanism into any other repo. Everything here is reusable: no repo-specific values live
in the scripts, only in `src/Directory.Build.props` (package metadata) and the workflows
(branch names).

## Repo layout

The scripts assume this standard layout (the only assumption they make):

```text
<repoRoot>/
├── .github/workflows/
│   ├── publish_nugets.yml     # build → test → bump → pack → push to nuget.org
│   └── update_nugets.yml      # daily: update packages → test → commit → publish
├── src/                       # packable libraries, one folder per project
│   ├── Directory.Build.props  # single <Version> + shared pack metadata
│   └── Directory.Build.targets
├── tests/                     # never packed, never versioned
│   └── Directory.Build.props  # IsPackable=false + shared test settings
├── pub/
│   ├── Invoke-VersionBump.ps1        # the ONE place the version is bumped
│   ├── PubVersion.json               # the version source of truth
│   └── lib/
│       ├── Update-VersionFile.ps1    # bump primitive (increments json, stamps props)
│       └── Update-NugetPackages.ps1  # upgrades all PackageReferences to latest
└── <Solution>.slnx
```

Naming conventions: repo folders are lowercase (`src`, `tests`, `pub`, `pub/lib`, `docs`);
PowerShell scripts are PascalCase Verb-Noun using [approved verbs]. Never rely on file-name
case — GitHub runners are case-sensitive (Linux), Windows is not.

[approved verbs]: https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands

## The Directory.Build pattern

MSBuild automatically imports the nearest `Directory.Build.props` (before the csproj body) and
`Directory.Build.targets` (after the csproj body) walking **up** from each project. That gives
three rules:

1. **`src/Directory.Build.props`** — everything shared and overridable: the single `<Version>`,
   language defaults (`Nullable`, `ImplicitUsings`, `LangVersion`), and all package metadata
   (authors, license, icon, readme, repo URLs, symbols). A csproj declares **only** what is unique
   to it: `Description`, `TargetFramework`, references. No `<Version>` in any csproj, ever.
2. **`src/Directory.Build.targets`** — anything conditioned on properties set in the csproj
   *body* (`IsPackable`, `TargetFramework`, …). Conditions on those in `.props` silently never
   match because `.props` is imported before the body runs. Example here: XML docs are generated
   for every packable project.
3. **`tests/Directory.Build.props`** — its existence *stops* the upward search, so test projects
   deliberately inherit nothing from `src/` (no pack metadata, no version) and set
   `IsPackable=false` once for every test project.

## Versioning: one source of truth

- `pub/PubVersion.json` holds `Version` (x.y.z), `BumpTime`, and `Prerelease`.
- `pub/lib/Update-VersionFile.ps1` is the only code that mutates it. On a bump it increments the
  build number and mirrors the version into the `<Version>` element of
  `src/Directory.Build.props`, so every package always shares one version.
- `pub/Invoke-VersionBump.ps1` is the only *entry point*: bump → commit
  (`chore: bump version x.y.z`) → push, no `--force` ever. It runs in CI so the bump never
  happens on a developer machine — that avoids version-file conflicts between developers.

Manual usage:

```powershell
./pub/Invoke-VersionBump.ps1            # stable bump (x.y.Z+1), commit, push
./pub/Invoke-VersionBump.ps1 -bump 2    # prerelease bump
./pub/Invoke-VersionBump.ps1 -noPush    # dry run: bump files only
```

## Publishing (`publish_nugets.yml`)

Runs on every push to `main` (and on manual dispatch). Order matters:

1. restore → build → **test** — a broken build/test fails the run *before* anything is committed
   or published, so it never burns a version.
2. `pub/Invoke-VersionBump.ps1` — bump + push the version commit (a push with `GITHUB_TOKEN`
   does not retrigger workflows, so this cannot loop).
3. `dotnet pack` (rebuilds at the new version) → `dotnet nuget push` to nuget.org.

Requires one repo/org secret: **`NUGET_API_KEY`**.

## Daily NuGet updates (`update_nugets.yml`)

Runs daily at 02:00 UTC (and on manual dispatch):

1. `pub/lib/Update-NugetPackages.ps1` upgrades every `PackageReference` under `src/` and `tests/`
   to the most recent version on nuget.org. It uses [dotnet-outdated] per project, so it does not
   depend on the solution file format. Stable versions only (a package already on a prerelease
   may move to a newer prerelease). Test projects therefore use explicit test PackageReferences
   (not `MSTest.Sdk`) — SDK-injected implicit references cannot be upgraded by the tool.
2. If nothing changed, the run ends green — no commit, no publish.
3. Otherwise: full build + **test** with the new packages. Only if they pass, the change is
   committed (`chore: update NuGet packages to latest`) and pushed to `main`.
4. It then dispatches `publish_nugets.yml`, which bumps the version and ships the updated
   packages.

**Failure = notification.** There is deliberately no fallback or auto-retry: if the update, the
build, the tests, or a push fails, the workflow run fails and GitHub emails the workflow author.
Nothing half-updated is ever committed. (Check Settings → Notifications → Actions if the emails
don't arrive; scheduled-run failure emails go to the user who last modified the workflow file.)

[dotnet-outdated]: https://github.com/dotnet-outdated/dotnet-outdated

## Adopting this in another repo (reusable workflows)

Both workflows are **reusable** (`workflow_call`): another repo carries no scripts and no CI
logic — only its version file and two thin wrappers that call this repo. The reusable workflow
checks the caller's repo out into `repo/` and always takes the scripts from this repo's `tools/`
checkout, so every consumer runs the same, single copy of the logic.

1. Lay the repo out as shown above (`src/`, `tests/`) plus `pub/PubVersion.json` — no `pub`
   scripts needed.
2. Copy `src/Directory.Build.props` / `src/Directory.Build.targets` /
   `tests/Directory.Build.props` and change only the package-metadata block (authors, license,
   icon, URLs) in `src/Directory.Build.props`. Set its `<Version>` to match `PubVersion.json`.
3. Strip every csproj down to `Description` + `TargetFramework` + references (delete `Version`,
   `FileVersion`, metadata, icon items — the props own them now).
4. Add the two thin wrappers. They MUST be named `publish_nugets.yml` and `update_nugets.yml`
   (the updater dispatches the publish wrapper by that name):

   ```yaml
   # .github/workflows/publish_nugets.yml
   name: Publish NuGets
   on:
     push:
       branches: [ "main" ]
     workflow_dispatch:
   permissions:
     contents: write
   jobs:
     publish-nugets:
       uses: graymint/GrayMint.Common/.github/workflows/publish_nugets.yml@main
       secrets: inherit
   ```

   ```yaml
   # .github/workflows/update_nugets.yml
   name: Update NuGets
   on:
     schedule:
       - cron: "0 2 * * *"
     workflow_dispatch:
   permissions:
     contents: write
     actions: write
   jobs:
     update-nugets:
       uses: graymint/GrayMint.Common/.github/workflows/update_nugets.yml@main
       secrets: inherit
   ```

5. Make sure the `NUGET_API_KEY` secret is available (org secret or repo secret).
6. Make sure there is at least one test project under `tests/` — the updater and the publisher
   both refuse to ship when tests fail, which is the whole safety net.

To bump or update such a repo manually from a local clone of this repo:

```powershell
./pub/Invoke-VersionBump.ps1 -repoDir C:\path\to\other-repo -noPush
./pub/lib/Update-NugetPackages.ps1 -repoDir C:\path\to\other-repo
```
