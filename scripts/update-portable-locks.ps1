[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    foreach ($runtime in @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')) {
        dotnet restore src/MailMeUp.Cli/MailMeUp.Cli.csproj -r $runtime --force-evaluate `
            -p:SelfContained=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:PublishTrimmed=false -p:MailMeUpPortableBuild=true
        if ($LASTEXITCODE -ne 0) { throw "Portable dependency restore failed: $runtime" }
    }
    dotnet restore MailMeUp.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Standard dependency restore failed.' }
} finally {
    Pop-Location
}
