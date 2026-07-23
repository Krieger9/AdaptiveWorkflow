<#
.SYNOPSIS
  Starts only the Vite UI for local frontend development.

.DESCRIPTION
  Runs AdaptiveTeamBuilderUI on http://localhost:5173.
  Use this when debugging the API in Visual Studio (or another IDE)
  so the backend is not started from this script.

.PARAMETER SkipNpmInstall
  Skip npm install when starting the UI.
#>
[CmdletBinding()]
param(
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$uiRoot = Join-Path $repoRoot 'src\AdaptiveTeamBuilderUI'

if (-not $SkipNpmInstall -or -not (Test-Path (Join-Path $uiRoot 'node_modules'))) {
    Write-Host "Ensuring UI dependencies..." -ForegroundColor Cyan
    Push-Location $uiRoot
    try {
        npm install
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

# Free the UI port if a previous run left something behind.
# Do not touch 5106 — Visual Studio may be hosting the API there.
Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue |
    ForEach-Object {
        Write-Host "Port 5173 in use; stopping PID $($_.OwningProcess)..." -ForegroundColor Yellow
        & taskkill.exe /PID $_.OwningProcess /T /F 2>$null | Out-Null
    }

Write-Host "Starting UI on http://localhost:5173 ..." -ForegroundColor Cyan
Write-Host "Expecting API from Visual Studio on http://localhost:5106" -ForegroundColor DarkGray
Write-Host "Press Ctrl+C to stop the UI.`n" -ForegroundColor DarkGray

Set-Location $uiRoot
npm run dev
