<#
.SYNOPSIS
  Runs the full local dev stack (API + UI) from one shell.

.EXAMPLE
  .\dev.ps1

.EXAMPLE
  .\dev.ps1 -Build

.EXAMPLE
  .\dev.ps1 -Build -PublishDb
#>
[CmdletBinding()]
param(
    [switch]$Build,
    [switch]$PublishDb,
    [switch]$SkipNpmInstall
)
& "$PSScriptRoot\scripts\dev.ps1" @PSBoundParameters
