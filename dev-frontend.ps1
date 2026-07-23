<#
.SYNOPSIS
  Starts only the Vite UI (no API). Useful when debugging the backend in Visual Studio.

.EXAMPLE
  .\dev-frontend.ps1

.EXAMPLE
  .\dev-frontend.ps1 -SkipNpmInstall
#>
[CmdletBinding()]
param(
    [switch]$SkipNpmInstall
)
& "$PSScriptRoot\scripts\dev-frontend.ps1" @PSBoundParameters
