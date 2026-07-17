# The ONE place the version is bumped.
#
# Increments pub/PubVersion.json (+ stamps src/Directory.Build.props), commits, and pushes the
# current branch. Intended to run in CI (.github/workflows/publish_nugets.yml) so the bump never
# happens on a developer's machine — that avoids version-file conflicts between developers. Can
# also be run locally for a manual bump. See docs/versioning-and-nuget-updates.md.
#
# Reusable: no repo-specific values in here. Works on any repo laid out as
# <repoRoot>/pub/PubVersion.json + <repoRoot>/src/Directory.Build.props. By default it targets
# the repo this script lives in; pass -repoDir to bump ANOTHER repo with the same layout (this is
# how the reusable workflow bumps a caller repo that carries no scripts of its own).
#
# Usage:
#   ./Invoke-VersionBump.ps1                    # stable release bump (x.y.Z + 1)
#   ./Invoke-VersionBump.ps1 -bump 2            # prerelease bump
#   ./Invoke-VersionBump.ps1 -noPush            # bump only, no commit/push (dry run)
#   ./Invoke-VersionBump.ps1 -repoDir C:\other  # bump another repo with the same layout

param(
	# 1 = stable release, 2 = prerelease. Any value > 0 increments the build number.
	[int]$bump = 1,
	# Bump only but do not commit or push.
	[switch]$noPush,
	# Root of the repo to bump; defaults to the repo containing this script.
	[string]$repoDir = (Split-Path -Parent $PSScriptRoot)
);

$ErrorActionPreference = "Stop";
$solutionDir = $repoDir;

# Dot-source so $versionParam/$versionTag/$prerelease land in this scope after the mutation.
. "$PSScriptRoot/lib/Update-VersionFile.ps1" -versionFile "$repoDir/pub/PubVersion.json" -bump $bump;

Write-Host "Bumped to $versionTag (prerelease=$prerelease)" -ForegroundColor Green;

if ($noPush) {
	Write-Host "noPush set: skipping commit/push." -ForegroundColor Yellow;
	return;
}

# Commit the bump and push the current branch. No --force ever: a non-fast-forward rejection
# signals a real divergence to reconcile by hand rather than overwrite.
git -C $solutionDir add -A;
git -C $solutionDir commit -m "chore: bump version $versionParam";
if ($LASTEXITCODE -ne 0) { throw "git commit failed (exit $LASTEXITCODE)"; }

git -C $solutionDir push origin HEAD;
if ($LASTEXITCODE -ne 0) { throw "git push failed (non-fast-forward? reconcile by hand, do not force) (exit $LASTEXITCODE)"; }
