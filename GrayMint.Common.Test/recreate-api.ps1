$projectDir = $PSScriptRoot;

nswag run "$projectDir/Api/Api.nswag"  `
	/variables:namespace=GrayMint.Common.Test.Api,projectDir=$projectDir/../GrayMint.Common.Test.WebApiSample/GrayMint.Common.Test.WebApiSample.csproj `
	/runtime:Net60