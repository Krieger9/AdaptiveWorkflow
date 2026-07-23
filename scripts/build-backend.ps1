<#
.SYNOPSIS
  Builds the .NET solution (API, Data, sqlproj).
#>
[CmdletBinding()]
param(
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

Write-Host "Building backend solution..." -ForegroundColor Cyan
if ($NoRestore) {
    dotnet build AdaptiveTeamBuilder.sln --no-restore
} else {
    dotnet build AdaptiveTeamBuilder.sln
}

if ($LASTEXITCODE -ne 0) {
    throw "Backend build failed with exit code $LASTEXITCODE."
}

Write-Host "Backend build succeeded." -ForegroundColor Green
