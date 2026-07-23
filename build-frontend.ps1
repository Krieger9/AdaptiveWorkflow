<#
.SYNOPSIS
  Builds the React frontend.
#>
[CmdletBinding()]
param(
    [switch]$SkipInstall
)
& "$PSScriptRoot\scripts\build-frontend.ps1" @PSBoundParameters
