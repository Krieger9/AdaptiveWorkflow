<#
.SYNOPSIS
  Builds the .NET backend solution.
#>
[CmdletBinding()]
param(
    [switch]$NoRestore
)
& "$PSScriptRoot\scripts\build-backend.ps1" @PSBoundParameters
