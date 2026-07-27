<#
.SYNOPSIS
  Resets collaboration user profiles in the local AdaptiveTeamBuilder database.

.DESCRIPTION
  Deletes rows from dbo.UserCollaborationStates. After reset, GET /api/collaboration/profile
  returns app defaults (no userOverride) until the profile updater learns again.

.PARAMETER Server
  SQL Server instance. Default: (localdb)\MSSQLLocalDB

.PARAMETER Database
  Target database name. Default: AdaptiveTeamBuilder

.PARAMETER UserId
  Optional Users.Id (GUID). When omitted, clears every collaboration profile.

.EXAMPLE
  .\scripts\reset-collaboration-profile.ps1

.EXAMPLE
  .\reset-collaboration-profile.ps1

.EXAMPLE
  .\scripts\reset-collaboration-profile.ps1 -UserId 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
#>
[CmdletBinding()]
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'AdaptiveTeamBuilder',
    [Guid]$UserId
)

$ErrorActionPreference = 'Stop'

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    throw "sqlcmd not found on PATH. Install SQL Server Command Line Utilities or use LocalDB tools."
}

if ($PSBoundParameters.ContainsKey('UserId')) {
    $filter = "WHERE [UserId] = '$UserId'"
    $scope = "user $UserId"
} else {
    $filter = ''
    $scope = 'all users'
}

Write-Host "Resetting collaboration profiles ($scope) on $Server / $Database ..."

$query = @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.UserCollaborationStates', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.UserCollaborationStates does not exist. Publish the database first (.\database\publish-local.ps1).', 16, 1);
    RETURN;
END

DECLARE @deleted INT;
DELETE FROM [dbo].[UserCollaborationStates] $filter;
SET @deleted = @@ROWCOUNT;
SELECT @deleted AS DeletedCount;
"@

$result = sqlcmd -S $Server -E -d $Database -h -1 -W -Q $query
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE."
}

$deleted = ($result | Where-Object { $_ -match '^\d+$' } | Select-Object -First 1)
if (-not $deleted) {
    $deleted = '0'
}

Write-Host "Deleted $deleted collaboration profile row(s). Reload Select Contract to see app defaults."
