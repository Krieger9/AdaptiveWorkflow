<#
.SYNOPSIS
  Resets local collaboration profiles and contract selection rotation stamps.

.EXAMPLE
  .\reset-collaboration-profile.ps1
#>
[CmdletBinding()]
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'AdaptiveTeamBuilder',
    [Guid]$UserId,
    [switch]$IncludeFileState
)
& "$PSScriptRoot\scripts\reset-collaboration-profile.ps1" @PSBoundParameters
