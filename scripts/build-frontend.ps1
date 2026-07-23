<#
.SYNOPSIS
  Installs npm deps (if needed) and builds the React UI.
#>
[CmdletBinding()]
param(
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$uiRoot = Join-Path $repoRoot 'src\AdaptiveTeamBuilderUI'
Set-Location $uiRoot

if (-not $SkipInstall -or -not (Test-Path (Join-Path $uiRoot 'node_modules'))) {
    Write-Host "Installing UI dependencies..." -ForegroundColor Cyan
    npm install
    if ($LASTEXITCODE -ne 0) {
        throw "npm install failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Building frontend..." -ForegroundColor Cyan
npm run build
if ($LASTEXITCODE -ne 0) {
    throw "Frontend build failed with exit code $LASTEXITCODE."
}

Write-Host "Frontend build succeeded." -ForegroundColor Green
