# Reusable version-bump primitive. Increments pub/PubVersion.json (the single source of truth for
# the product version) and mirrors the new version into src/Directory.Build.props — the one
# <Version> every packable project inherits. Per-csproj <Version> stamping is retired.
#
# Layout assumption (the only one): this repo is laid out as
#   <repoRoot>/pub/PubVersion.json
#   <repoRoot>/src/Directory.Build.props   (contains a <Version>x.y.z</Version> element)
#
# Dot-source this script to ALSO get the version variables in the caller's scope without bumping
# (bump = 0 reads only): $versionParam (x.y.z), $versionTag (vx.y.z[-prerelease]), $prerelease,
# $versionCode (the Build component).
param(
	[String]$versionFile,
	# 0 = read only, 1 = stable release bump, 2 = prerelease bump. Any value > 0 increments the build number.
	[int]$bump = 0
)

$versionJson = (Get-Content $versionFile | Out-String | ConvertFrom-Json);
$version = [version]::Parse($versionJson.Version);

if ($bump -gt 0) {
	$version = [version]::new($version.Major, $version.Minor, $version.Build + 1);
	$versionJson.Version = $version.ToString(3);
	$versionJson.BumpTime = [datetime]::UtcNow.ToString("o");
	$versionJson.Prerelease = ($bump -eq 2);
	$versionJson | ConvertTo-Json -Depth 10 | Out-File $versionFile;

	# Mirror the version into src/Directory.Build.props (path resolved relative to the version file).
	$srcPropsFile = Join-Path (Split-Path -Parent (Split-Path -Parent $versionFile)) "src/Directory.Build.props";
	if (Test-Path $srcPropsFile) {
		$props = Get-Content $srcPropsFile -Raw;
		$props = ([regex]"<Version>.*?</Version>").Replace($props, "<Version>$($versionJson.Version)</Version>", 1);
		Set-Content -Path $srcPropsFile -Value $props -Encoding utf8 -NoNewline;
	}
	else {
		throw "Could not find $srcPropsFile to stamp the version into.";
	}

	Write-Host "Version has been bumped to: $($versionJson.Version)" -ForegroundColor Blue;
}

$prerelease = $versionJson.Prerelease -eq $true;
$versionCode = $version.Build;
$versionParam = $version.ToString(3);
$versionTag = "v$versionParam" + $(if ($prerelease) { "-prerelease" } else { "" });
