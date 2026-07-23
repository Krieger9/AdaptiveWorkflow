<#
.SYNOPSIS
  Builds backend and frontend.
#>
[CmdletBinding()]
param(
    [switch]$SkipFrontendInstall
)

$ErrorActionPreference = 'Stop'
$scripts = $PSScriptRoot

& (Join-Path $scripts 'build-backend.ps1')
& (Join-Path $scripts 'build-frontend.ps1') -SkipInstall:$SkipFrontendInstall

Write-Host "Full build completed." -ForegroundColor Green
