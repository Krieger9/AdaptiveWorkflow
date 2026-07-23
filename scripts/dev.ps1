<#
.SYNOPSIS
  Starts the local dev environment from one shell: API + Vite UI.

.DESCRIPTION
  Launches AdaptiveTeamBuilderSvc (http://localhost:5106) in the background,
  waits for /health, then runs the Vite UI in the foreground (http://localhost:5173).
  Ctrl+C stops both.

.PARAMETER Build
  Build backend (and optionally frontend) before starting.

.PARAMETER PublishDb
  Run database\publish-local.ps1 before starting.

.PARAMETER SkipNpmInstall
  Skip npm install when starting the UI.
#>
[CmdletBinding()]
param(
    [switch]$Build,
    [switch]$PublishDb,
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$uiRoot = Join-Path $repoRoot 'src\AdaptiveTeamBuilderUI'
$apiProject = Join-Path $repoRoot 'src\AdaptiveTeamBuilderSvc\AdaptiveTeamBuilderSvc.csproj'
$apiLog = Join-Path $env:TEMP 'adaptive-team-builder-api.out.log'
$apiErrLog = Join-Path $env:TEMP 'adaptive-team-builder-api.err.log'
$apiProcess = $null

function Stop-DevProcesses {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Write-Host "`nStopping API (PID $($apiProcess.Id))..." -ForegroundColor Yellow
        & taskkill.exe /PID $apiProcess.Id /T /F 2>$null | Out-Null
    }
}

if ($PublishDb) {
    Write-Host "Publishing local database..." -ForegroundColor Cyan
    & (Join-Path $repoRoot 'database\publish-local.ps1')
}

if ($Build) {
    & (Join-Path $PSScriptRoot 'build-backend.ps1')
}

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

# Free ports if a previous run left something behind.
foreach ($port in 5106, 5173) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        ForEach-Object {
            Write-Host "Port $port in use; stopping PID $($_.OwningProcess)..." -ForegroundColor Yellow
            & taskkill.exe /PID $_.OwningProcess /T /F 2>$null | Out-Null
        }
}

Write-Host "Starting API on http://localhost:5106 ..." -ForegroundColor Cyan
Remove-Item $apiLog, $apiErrLog -Force -ErrorAction SilentlyContinue

$apiProcess = Start-Process `
    -FilePath 'dotnet' `
    -ArgumentList @('run', '--project', $apiProject, '--launch-profile', 'http') `
    -WorkingDirectory $repoRoot `
    -RedirectStandardOutput $apiLog `
    -RedirectStandardError $apiErrLog `
    -PassThru `
    -WindowStyle Hidden

function Show-ApiLogs {
    if (Test-Path $apiLog) {
        Write-Host "--- API stdout ---" -ForegroundColor DarkGray
        Get-Content $apiLog
    }
    if (Test-Path $apiErrLog) {
        Write-Host "--- API stderr ---" -ForegroundColor DarkGray
        Get-Content $apiErrLog
    }
}

try {
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        if ($apiProcess.HasExited) {
            Write-Host "API exited early. Log:" -ForegroundColor Red
            Show-ApiLogs
            throw "API failed to start."
        }

        try {
            $health = Invoke-RestMethod -Uri 'http://localhost:5106/health' -TimeoutSec 2
            if ($health.status -eq 'healthy') {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $ready) {
        Write-Host "API did not become healthy in time. Log:" -ForegroundColor Red
        Show-ApiLogs
        throw "Timed out waiting for API health."
    }

    Write-Host "API is healthy." -ForegroundColor Green
    Write-Host "Starting UI on http://localhost:5173 ..." -ForegroundColor Cyan
    Write-Host "API logs: $apiLog / $apiErrLog" -ForegroundColor DarkGray
    Write-Host "Press Ctrl+C to stop API + UI.`n" -ForegroundColor DarkGray

    Set-Location $uiRoot
    npm run dev
}
finally {
    Stop-DevProcesses
}
