<#
.SYNOPSIS
  Resets local collaboration profiles (clears UserCollaborationStates).

.EXAMPLE
  .\reset-collaboration-profile.ps1
#>
[CmdletBinding()]
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'AdaptiveTeamBuilder',
    [Guid]$UserId
)
& "$PSScriptRoot\scripts\reset-collaboration-profile.ps1" @PSBoundParameters
