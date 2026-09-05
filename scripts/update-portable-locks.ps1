[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    foreach ($runtime in @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')) {
        dotnet restore src/MailMeUp.Cli/MailMeUp.Cli.csproj "-p:RuntimeIdentifier=$runtime" --force-evaluate `
            -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:PublishTrimmed=false -p:MailMeUpPortableBuild=true "-p:MailMeUpPortableRuntime=$runtime"
        if ($LASTEXITCODE -ne 0) { throw "Portable dependency restore failed: $runtime" }
        foreach ($lockFile in Get-ChildItem -LiteralPath "eng/locks/$runtime" -Filter '*.json') {
            $graph = Get-Content -LiteralPath $lockFile.FullName -Raw | ConvertFrom-Json
            $unexpected = $graph.dependencies.PSObject.Properties.Name | Where-Object { $_.Contains('/') -and -not $_.EndsWith("/$runtime") }
            if ($unexpected) { throw "Portable graph includes an unexpected host runtime: $($lockFile.Name)" }
        }
    }
    dotnet restore MailMeUp.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Standard dependency restore failed.' }
} finally {
    Pop-Location
}
