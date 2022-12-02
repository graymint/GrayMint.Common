$curDir = $PSScriptRoot;
$solutionDir = (Split-Path $PSScriptRoot -Parent);
$projectFile="$solutionDir\GrayMint.Common.Test.WebApiSample\GrayMint.Common.Test.WebApiSample.csproj";
$namespace = "GrayMint.Common.Test.Api";

$nswag = "${Env:ProgramFiles(x86)}\Rico Suter\NSwagStudio\Net70\dotnet-nswag.exe";
$variables="/variables:namespace=$namespace,apiFile=Api.cs,projectFile=$projectFile";


& "$nswag" run "$curDir/Api/Api.nswag" $variables /runtime:Net70;