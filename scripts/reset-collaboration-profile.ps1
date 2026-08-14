<#
.SYNOPSIS
  Resets collaboration user profiles in the local AdaptiveTeamBuilder database.

.DESCRIPTION
  Deletes rows from dbo.UserCollaborationStates, dbo.CollaborationTurnDigests and
  dbo.CollaborationStateChangeLogs, and clears Contracts.LastSelectedAt so the Select
  Contract demo rotation returns to the designed DemoSortOrder head.
  After reset, GET /api/collaboration/profile returns app defaults (no userOverride)
  until the profile updater learns again.

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

Write-Host "Resetting collaboration profiles ($scope) and contract selection stamps on $Server / $Database ..."

$query = @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.UserCollaborationStates', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.UserCollaborationStates does not exist. Publish the database first (.\database\publish-local.ps1).', 16, 1);
    RETURN;
END

DECLARE @deleted INT;
DECLARE @deletedDigests INT = 0;
DECLARE @deletedChangeLogs INT = 0;

-- Delete change logs first (FK to turn digests), then turn digests, then states.
IF OBJECT_ID(N'dbo.CollaborationStateChangeLogs', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[CollaborationStateChangeLogs] $filter;
    SET @deletedChangeLogs = @@ROWCOUNT;
END

IF OBJECT_ID(N'dbo.CollaborationTurnDigests', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[CollaborationTurnDigests] $filter;
    SET @deletedDigests = @@ROWCOUNT;
END

DELETE FROM [dbo].[UserCollaborationStates] $filter;
SET @deleted = @@ROWCOUNT;

DECLARE @cleared INT = 0;
IF COL_LENGTH(N'dbo.Contracts', N'LastSelectedAt') IS NOT NULL
BEGIN
    UPDATE [dbo].[Contracts] SET [LastSelectedAt] = NULL WHERE [LastSelectedAt] IS NOT NULL;
    SET @cleared = @@ROWCOUNT;
END

SELECT @deleted AS DeletedCount, @cleared AS ClearedSelectionStamps, @deletedDigests AS DeletedDigests, @deletedChangeLogs AS DeletedChangeLogs;
"@

$result = sqlcmd -S $Server -E -d $Database -h -1 -W -Q $query
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE."
}

$nums = @($result | Where-Object { $_ -match '^\d+$' })
$deleted = if ($nums.Count -ge 1) { $nums[0] } else { '0' }
$cleared = if ($nums.Count -ge 2) { $nums[1] } else { '0' }
$deletedDigests = if ($nums.Count -ge 3) { $nums[2] } else { '0' }
$deletedChangeLogs = if ($nums.Count -ge 4) { $nums[3] } else { '0' }

Write-Host "Deleted $deleted collaboration profile row(s); cleared $cleared contract LastSelectedAt stamp(s); deleted $deletedDigests turn digest row(s); deleted $deletedChangeLogs change-log row(s)."
Write-Host "Reload Select Contract to see app defaults and the designed DemoSortOrder trio."
