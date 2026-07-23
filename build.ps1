<#
.SYNOPSIS
  Builds backend and frontend.
#>
[CmdletBinding()]
param(
    [switch]$SkipFrontendInstall
)
& "$PSScriptRoot\scripts\build.ps1" @PSBoundParameters
