[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repositoryRoot 'src\GBFR.InfinityFullAssist\GBFR.InfinityFullAssist.csproj'
$sourceReadme = Join-Path $repositoryRoot 'README.md'
$sourceChineseReadme = Join-Path $repositoryRoot 'README.zh-CN.md'
$buildOutput = Join-Path $repositoryRoot "src\GBFR.InfinityFullAssist\bin\$Configuration"
$artifactRoot = Join-Path $repositoryRoot 'artifacts\release'
$stagingRoot = Join-Path $artifactRoot 'gbfr.qol.infinityfullassist'

dotnet build $projectPath --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$metadataPath = Join-Path $buildOutput 'ModConfig.json'
$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
$archivePath = Join-Path $artifactRoot (
    "Infinity-Assist-Unlock-$($metadata.ModVersion).zip")

New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

foreach ($target in @($stagingRoot, $archivePath)) {
    $fullTarget = [System.IO.Path]::GetFullPath($target)
    $fullArtifactRoot = [System.IO.Path]::GetFullPath($artifactRoot) +
        [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullTarget.StartsWith(
            $fullArtifactRoot,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a path outside the release artifact root: $fullTarget"
    }

    if (Test-Path -LiteralPath $fullTarget) {
        Remove-Item -LiteralPath $fullTarget -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $stagingRoot | Out-Null
New-Item -ItemType Directory -Path (
    Join-Path $stagingRoot 'Signatures') | Out-Null

$rootFiles = @(
    'GBFR.InfinityFullAssist.deps.json',
    'GBFR.InfinityFullAssist.dll',
    'gbfrelink.utility.manager.Interfaces.dll',
    'ModConfig.json',
    'NenTools.Reloaded.ScanManager.Interfaces.dll',
    'Preview.png',
    'Reloaded.Hooks.Definitions.dll',
    'Reloaded.Hooks.ReloadedII.Interfaces.dll',
    'Reloaded.Memory.dll',
    'Reloaded.Memory.Sigscan.Definitions.dll',
    'Reloaded.Memory.SigScan.ReloadedII.Interfaces.dll'
)

foreach ($name in $rootFiles) {
    Copy-Item -LiteralPath (Join-Path $buildOutput $name) -Destination $stagingRoot
}

Copy-Item -LiteralPath $sourceReadme -Destination (
    Join-Path $stagingRoot 'README.md')
Copy-Item -LiteralPath $sourceChineseReadme -Destination (
    Join-Path $stagingRoot 'README.zh-CN.md')
Copy-Item -LiteralPath (
    Join-Path $buildOutput 'Signatures\granblue_fantasy_relink_er.ini'
) -Destination (Join-Path $stagingRoot 'Signatures')

$expectedEntries = @(
    $rootFiles
    'README.md'
    'README.zh-CN.md'
    'Signatures/granblue_fantasy_relink_er.ini'
) | Sort-Object

$stagedEntries = Get-ChildItem -LiteralPath $stagingRoot -Recurse -File |
    ForEach-Object {
        $_.FullName.Substring($stagingRoot.Length + 1).Replace('\', '/')
    } |
    Sort-Object

if (Compare-Object -ReferenceObject $expectedEntries -DifferenceObject $stagedEntries) {
    throw 'Release staging content does not match the approved manifest.'
}

Compress-Archive -Path (
    Join-Path $stagingRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression
$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $archiveEntries = $archive.Entries |
        Where-Object { -not [string]::IsNullOrEmpty($_.Name) } |
        ForEach-Object { $_.FullName.Replace('\', '/') } |
        Sort-Object
}
finally {
    $archive.Dispose()
}

if (Compare-Object -ReferenceObject $expectedEntries -DifferenceObject $archiveEntries) {
    throw 'Release archive content does not match the approved manifest.'
}

[pscustomobject]@{
    Archive = $archivePath
    Version = $metadata.ModVersion
    Entries = $archiveEntries.Count
}
