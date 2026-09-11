#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Publishes each EFCore_DaIn database provider (self-contained) for Windows and Linux
    into docs\providers10.0.11\<ProviderName>\<rid>.

.DESCRIPTION
    Discovers every DainEF.Providers.* project under src\EFCore_DaIn\10.0.11 and runs
    `dotnet publish` for each one, once per target runtime identifier (win-x64, linux-x64),
    self-contained, placing the output under the repository's docs folder so the published
    binaries can be inspected or distributed per platform. Each publish output folder is then
    compressed into a .zip file, and the uncompressed folder is removed so only the zip remains.
    Finally, a providers.json manifest listing every published provider and its runtime zips
    is written into the docs\providers10.0.11 folder.

.PARAMETER Configuration
    Build configuration to publish (default: Release).

.PARAMETER Runtimes
    Runtime identifiers to publish for (default: win-x64, linux-x64).

.EXAMPLE
    ./publish-providers.ps1
    ./publish-providers.ps1 -Configuration Debug -Runtimes win-x64
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string[]]$Runtimes = @('win-x64', 'linux-x64')
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$providersRoot = Join-Path $repoRoot 'src\EFCore_DaIn\10.0.11'
$docsRoot = Join-Path $repoRoot 'docs\providers10.0.11'

if (-not (Test-Path $providersRoot)) {
    throw "Providers folder not found: $providersRoot"
}

$providerProjects = Get-ChildItem -Path $providersRoot -Directory -Filter 'DainEF.Providers.*' |
    ForEach-Object {
        $csproj = Get-ChildItem -Path $_.FullName -Filter '*.csproj' | Select-Object -First 1
        if ($csproj) {
            [PSCustomObject]@{
                Name    = $_.Name
                Project = $csproj.FullName
            }
        }
    }

if (-not $providerProjects) {
    throw "No provider projects found under $providersRoot"
}

Write-Host "Found $($providerProjects.Count) provider project(s):" -ForegroundColor Cyan
$providerProjects | ForEach-Object { Write-Host "  - $($_.Name)" }

$failures = @()
$manifestProviders = @()

foreach ($provider in $providerProjects) {
    $runtimeEntries = @()

    foreach ($rid in $Runtimes) {
        $outputDir = Join-Path (Join-Path $docsRoot $provider.Name) $rid

        Write-Host ""
        Write-Host "Publishing $($provider.Name) for $rid -> $outputDir" -ForegroundColor Yellow

        $args = @(
            'publish'
            $provider.Project
            '-c', $Configuration
            '-r', $rid
            '--self-contained', 'true'
            '-o', $outputDir
        )

        & dotnet @args
        if ($LASTEXITCODE -ne 0) {
            Write-Host "FAILED: $($provider.Name) ($rid)" -ForegroundColor Red
            $failures += "$($provider.Name) ($rid)"
            continue
        }

        Write-Host "OK: $($provider.Name) ($rid)" -ForegroundColor Green

        $zipPath = "$outputDir.zip"
        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }

        Write-Host "Zipping -> $zipPath" -ForegroundColor Yellow
        Compress-Archive -Path (Join-Path $outputDir '*') -DestinationPath $zipPath -Force
        Remove-Item -Path $outputDir -Recurse -Force
        Write-Host "Zipped: $zipPath" -ForegroundColor Green

        $runtimeEntries += [PSCustomObject]@{
            rid = $rid
            zip = (Resolve-Path $zipPath).Path.Substring($docsRoot.Length + 1) -replace '\\', '/'
        }
    }

    $manifestProviders += [PSCustomObject]@{
        name     = $provider.Name
        project  = (Resolve-Path $provider.Project).Path.Substring($repoRoot.Length + 1) -replace '\\', '/'
        runtimes = $runtimeEntries
    }
}

$manifest = [PSCustomObject]@{
    generatedAt = (Get-Date).ToString('o')
    configuration = $Configuration
    runtimes    = $Runtimes
    providers   = $manifestProviders
}

$manifestPath = Join-Path $docsRoot 'providers.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding utf8
Write-Host ""
Write-Host "Wrote provider manifest -> $manifestPath" -ForegroundColor Cyan

Write-Host ""
if ($failures.Count -gt 0) {
    Write-Host "Completed with failures:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
else {
    Write-Host "All providers published successfully to $docsRoot" -ForegroundColor Green
}
