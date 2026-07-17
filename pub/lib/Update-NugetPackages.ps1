# Updates every PackageReference under src/ and tests/ to the most recent version on nuget.org,
# using dotnet-outdated (https://github.com/dotnet-outdated/dotnet-outdated). Stable packages only
# — a package currently on a prerelease version may move to a newer prerelease (dotnet-outdated's
# "Auto" pre-release policy).
#
# Reusable: no repo-specific values in here. Copy pub/ into any repo laid out as
# <repoRoot>/pub with projects under src/ and tests/ and it works unchanged. The default
# repo-root parameter assumes this script lives in <repoRoot>/pub/lib.
#
# Any failure throws (non-zero exit) so CI fails loudly instead of publishing half-updated
# packages. It does NOT build, test, commit, or push — the caller (the update_nugets workflow)
# owns that, so the update can be validated by the test suite before anything is committed.
#
# In GitHub Actions it writes "updated=true|false" to $GITHUB_OUTPUT so later steps can be
# conditioned on whether anything actually changed.

param(
	# Repo root; the default assumes this script lives in <repoRoot>/pub/lib.
	[string]$repoDir = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

$ErrorActionPreference = "Stop";

# Install (or update) the tool; `dotnet tool update` is a no-op install when already current.
dotnet tool update --global dotnet-outdated-tool;
if ($LASTEXITCODE -ne 0) { throw "Failed to install dotnet-outdated-tool (exit $LASTEXITCODE)"; }

# Run per project (not per solution) so it never depends on the solution file format.
$projectDirs = @("src", "tests") | ForEach-Object { Join-Path $repoDir $_ } | Where-Object { Test-Path $_ };
$projects = Get-ChildItem -Path $projectDirs -Recurse -Filter *.csproj -File;
if (!$projects) { throw "No csproj files found under: $($projectDirs -join ', ')"; }

foreach ($project in $projects) {
	Write-Host "Checking $($project.Name) ..." -ForegroundColor Cyan;
	dotnet outdated $project.FullName --upgrade;
	if ($LASTEXITCODE -ne 0) { throw "dotnet outdated failed for $($project.Name) (exit $LASTEXITCODE)"; }
}

# Report whether anything changed (workflow decides what to do with it).
$changes = git -C $repoDir status --porcelain;
if ($LASTEXITCODE -ne 0) { throw "git status failed (exit $LASTEXITCODE)"; }
$updated = ![string]::IsNullOrWhiteSpace(($changes | Out-String).Trim());

if ($updated) { Write-Host "Packages were updated:" -ForegroundColor Green; $changes | Write-Host; }
else { Write-Host "Everything is already up to date." -ForegroundColor Green; }

if ($env:GITHUB_OUTPUT) {
	"updated=$($updated.ToString().ToLower())" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8;
}
