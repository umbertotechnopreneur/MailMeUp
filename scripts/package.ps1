[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string]$Runtime
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$repoRoot = Split-Path -Parent $PSScriptRoot
[xml]$properties = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props')
$version = $properties.Project.PropertyGroup.Version
$artifactRoot = Join-Path $repoRoot 'artifacts'
$packageName = "mailmeup-$version-$Runtime"
$payload = Join-Path $artifactRoot $packageName
if (Test-Path -LiteralPath $payload) { throw "Package directory already exists: $payload. Move it aside before packaging again." }

$hostOs = if ($IsWindows) { 'win' } elseif ($IsMacOS) { 'osx' } else { 'linux' }
if (-not $Runtime.StartsWith("$hostOs-")) { throw 'Package on the target operating system to preserve archive and executable behavior.' }
$hostArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$smokeStatus = if ($Runtime -eq "$hostOs-$hostArchitecture") { 'native' } else { 'not-run-cross-architecture' }

Push-Location $repoRoot
try {
    $commit = git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'A Git commit is required for package provenance.' }
    $dirty = git status --porcelain --untracked-files=normal
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Git state.' }
    if ($dirty) { throw 'Commit or move aside repository changes before packaging. Artifacts are ignored.' }

    dotnet publish src/MailMeUp.Cli/MailMeUp.Cli.csproj -c Release -r $Runtime --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false `
        -p:DebugType=None -p:DebugSymbols=false -p:GenerateDocumentationFile=false `
        -p:ContinuousIntegrationBuild=true -p:RestoreLockedMode=true -p:RestorePackagesWithLockFile=false --output $payload
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

    Copy-Item -LiteralPath LICENSE, THIRD_PARTY_NOTICES.md, docs/DEPENDENCIES.md -Destination $payload
    Copy-Item -LiteralPath docs/RELEASE_README.md -Destination (Join-Path $payload 'README.md')
    Copy-Item -LiteralPath docs/licenses -Destination (Join-Path $payload 'licenses') -Recurse
    python scripts/export-notices.py --check --copy-to (Join-Path $payload 'licenses/packages')
    if ($LASTEXITCODE -ne 0) { throw 'Notice export failed.' }

    $executable = Join-Path $payload $(if ($IsWindows) { 'mailmeup.exe' } else { 'mailmeup' })
    if ($smokeStatus -eq 'native') {
        python scripts/smoke-test.py $executable
        if ($LASTEXITCODE -ne 0) { throw 'Published executable smoke test failed.' }
    }
    "Version=$version`nRuntime=$Runtime`nCommit=$commit`nSmokeTest=$smokeStatus`nStage=foundation" |
        Set-Content -LiteralPath (Join-Path $payload 'BUILD_INFO.txt') -Encoding utf8NoBOM

    $extension = if ($IsWindows) { 'zip' } else { 'tar.gz' }
    $archive = Join-Path $artifactRoot "$packageName.$extension"
    if (Test-Path -LiteralPath $archive) { throw "Archive already exists: $archive" }
    if ($IsWindows) {
        Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $archive
    } else {
        tar -czf $archive -C $payload .
        if ($LASTEXITCODE -ne 0) { throw 'Archive creation failed.' }
    }
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $archive)" | Set-Content -LiteralPath "$archive.sha256" -Encoding utf8NoBOM
    Write-Output "Created $archive (smoke: $smokeStatus)"
} finally {
    Pop-Location
}
