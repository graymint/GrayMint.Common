# Packs each API client of a repo whose code changed since its newest version on nuget.org into $outputDir: the
# reusable pack_api_clients.yml runs it, and the repo's own publish workflow pushes what it packs.
#
# An API client is a packable project that references GrayMint.Common.OpenApi.ClientGenerator. Its build writes the
# client again from its API first, so the client is compared as the API makes it now. A package records the commit it
# was built from (its nuspec's repository commit), and the client's folder at that commit is compared with the folder
# now, line endings aside. A version that records no commit, or one this history lacks, counts as changed, and so does
# a file git does not track yet.
#
# The version is the newest version's next patch (1.0.0 for the first), or the project's own <Version> where that is
# higher: a project sets <Version>2.0.0</Version> to start a new major version.
#
# Try it locally: ./pub/Build-ApiClientPackages.ps1 -repoDir <repo> -outputDir $env:TEMP/nupkgs
# -feed takes a folder laid out as nuget.org's flat container (<id>/index.json, <id>/<version>/<id>.nuspec) instead.

param(
	[Parameter(Mandatory = $true)][string]$repoDir,
	[string]$outputDir = "nupkgs",
	[string]$configuration = "Release",
	[string]$feed = "https://api.nuget.org/v3-flatcontainer"
);

$ErrorActionPreference = "Stop";
$PSNativeCommandUseErrorActionPreference = $false; # git diff --quiet answers a change with exit code 1
$repoDir = (Resolve-Path $repoDir).Path;
$generatorId = "GrayMint.Common.OpenApi.ClientGenerator";

# a file of the feed as text, or $null when the feed has none
function Read-Feed([string]$path) {
	if ($feed -notmatch '^https?://') {
		$file = Join-Path $feed $path;
		return (Test-Path $file) ? [IO.File]::ReadAllText($file) : $null;
	}

	$response = Invoke-WebRequest "$feed/$path" -SkipHttpErrorCheck;
	if ($response.StatusCode -eq 404) { return $null }
	if ($response.StatusCode -ne 200) { throw "The feed answered $($response.StatusCode) for $path." }
	$content = $response.Content;
	return ($content -is [byte[]]) ? [Text.Encoding]::UTF8.GetString($content) : $content;
}

# whether the client's folder differs from the one at $commit (git diff --quiet: exit code 1 for a difference)
function Test-Changed([string]$commit, [string]$relDir) {
	git -C $repoDir diff --quiet --ignore-cr-at-eol $commit -- $relDir;
	$diffCode = $LASTEXITCODE;
	if ($diffCode -gt 1) { throw "git diff failed for $relDir." }
	$untracked = git -C $repoDir ls-files --others --exclude-standard -- $relDir;
	return ($diffCode -eq 1) -or [bool]$untracked;
}

$head = git -C $repoDir rev-parse HEAD;
if ($LASTEXITCODE -ne 0) { throw "$repoDir is not a git checkout." }
New-Item -ItemType Directory -Force $outputDir | Out-Null;
$outputDir = (Resolve-Path $outputDir).Path;

# a project that takes the generator as a package, or imports its targets
$generatorReference = '(Include|Project)="[^"]*' + [regex]::Escape($generatorId);
$projects = Get-ChildItem $repoDir -Recurse -Filter *.csproj -File |
	Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules|\.git)[\\/]' } |
	Where-Object { (Get-Content -Raw $_.FullName) -match $generatorReference };

foreach ($project in $projects) {
	$properties = (dotnet msbuild $project.FullName -getProperty:IsPackable -getProperty:PackageId -getProperty:Version |
		ConvertFrom-Json).Properties;
	if ($LASTEXITCODE -ne 0) { throw "Reading $($project.Name) failed." }
	if ($properties.IsPackable -ne "true") {
		Write-Host "$($project.BaseName) is not packed (IsPackable is not true).";
		continue;
	}

	# the build writes the client again from its API
	$id = $properties.PackageId;
	Write-Host "Building $id..." -ForegroundColor Blue;
	dotnet build $project.FullName --configuration $configuration;
	if ($LASTEXITCODE -ne 0) { throw "Building $id failed." }

	$relDir = [IO.Path]::GetRelativePath($repoDir, $project.DirectoryName).Replace('\', '/');
	if (Test-Changed $head $relDir) {
		$message = "$id's committed client is not what its API makes now: build it and commit its files.";
		Write-Warning $message;
		if ($env:GITHUB_ACTIONS -eq "true") { Write-Host "::warning::$message" }
	}

	$lowerId = $id.ToLowerInvariant();
	$index = Read-Feed "$lowerId/index.json";
	$latest = $index ? (($index | ConvertFrom-Json).versions | Where-Object { $_ -notmatch '-' } |
		ForEach-Object { [version]$_ } | Sort-Object | Select-Object -Last 1) : $null;

	if ($latest) {
		$nuspec = [xml](Read-Feed "$lowerId/$latest/$lowerId.nuspec").TrimStart([char]0xFEFF);
		$commit = $nuspec.package.metadata.repository.commit;
		if ($commit -and (git -C $repoDir cat-file -t $commit 2>$null) -eq "commit" -and -not (Test-Changed $commit $relDir)) {
			Write-Host "$id $latest is current: its client is as at $commit.";
			continue;
		}
	}

	$next = $latest ? [version]"$($latest.Major).$($latest.Minor).$($latest.Build + 1)" : [version]"1.0.0";
	$own = [version](($properties.Version -replace '-.*$', ''));
	$version = ($own -gt $next) ? $own : $next;
	$version = "$($version.Major).$($version.Minor).$([Math]::Max($version.Build, 0))";

	Write-Host "Packing $id $version..." -ForegroundColor Blue;
	dotnet pack $project.FullName --configuration $configuration --output $outputDir -p:Version=$version `
		-p:RepositoryType=git -p:RepositoryCommit=$head;
	if ($LASTEXITCODE -ne 0) { throw "Packing $id failed." }
}
