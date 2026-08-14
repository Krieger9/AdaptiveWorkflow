<#
.SYNOPSIS
  Resets collaboration belief profiles in the local AdaptiveTeamBuilder database.

.DESCRIPTION
  Deletes rows from dbo.Revisions, dbo.TurnDigests, dbo.Beliefs, dbo.BeliefDocuments
  and dbo.Interactions, drops legacy pre-framework tables (dbo.CollaborationStateChangeLogs,
  dbo.CollaborationTurnDigests, dbo.UserCollaborationStates) if they still exist, and
  clears Contracts.LastSelectedAt so the Select Contract
  demo rotation returns to the designed DemoSortOrder head.
  After reset, GET /api/collaboration/profile returns the seeded default belief
  document until the profile updater learns again.

  Note: file-based framework state (JSONL interaction logs, profile version archive,
  run records, shadow counters) lives under src/AdaptiveTeamBuilderSvc/data/ and is
  cleared with -IncludeFileState.

.PARAMETER Server
  SQL Server instance. Default: (localdb)\MSSQLLocalDB

.PARAMETER Database
  Target database name. Default: AdaptiveTeamBuilder

.PARAMETER UserId
  Optional Users.Id (GUID). When omitted, clears every collaboration profile.

.PARAMETER IncludeFileState
  Also delete file-based state (interactions/, profiles/, runs/, shadow-counters/)
  under src/AdaptiveTeamBuilderSvc/data. Glossary files are kept.

.EXAMPLE
  .\scripts\reset-collaboration-profile.ps1

.EXAMPLE
  .\scripts\reset-collaboration-profile.ps1 -UserId 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'

.EXAMPLE
  .\scripts\reset-collaboration-profile.ps1 -IncludeFileState
#>
[CmdletBinding()]
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'AdaptiveTeamBuilder',
    [Guid]$UserId,
    [switch]$IncludeFileState
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

Write-Host "Resetting collaboration belief profiles ($scope) and contract selection stamps on $Server / $Database ..."

$query = @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.BeliefDocuments', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.BeliefDocuments does not exist. Publish the database first (.\database\publish-local.ps1).', 16, 1);
    RETURN;
END

DECLARE @deletedDocs INT = 0;
DECLARE @deletedBeliefs INT = 0;
DECLARE @deletedDigests INT = 0;
DECLARE @deletedRevisions INT = 0;
DECLARE @deletedInteractions INT = 0;

-- Delete revisions first (FK to turn digests), then digests, beliefs, documents, interactions.
IF OBJECT_ID(N'dbo.Revisions', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[Revisions] $filter;
    SET @deletedRevisions = @@ROWCOUNT;
END

IF OBJECT_ID(N'dbo.TurnDigests', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[TurnDigests] $filter;
    SET @deletedDigests = @@ROWCOUNT;
END

IF OBJECT_ID(N'dbo.Beliefs', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[Beliefs] $filter;
    SET @deletedBeliefs = @@ROWCOUNT;
END

DELETE FROM [dbo].[BeliefDocuments] $filter;
SET @deletedDocs = @@ROWCOUNT;

IF OBJECT_ID(N'dbo.Interactions', N'U') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[Interactions] $filter;
    SET @deletedInteractions = @@ROWCOUNT;
END

-- Drop legacy tables from the pre-framework schema (replaced by Revisions, TurnDigests,
-- and BeliefDocuments). sqlpackage publish leaves objects missing from the source project.
IF OBJECT_ID(N'dbo.CollaborationStateChangeLogs', N'U') IS NOT NULL DROP TABLE [dbo].[CollaborationStateChangeLogs];
IF OBJECT_ID(N'dbo.CollaborationTurnDigests', N'U') IS NOT NULL DROP TABLE [dbo].[CollaborationTurnDigests];
IF OBJECT_ID(N'dbo.UserCollaborationStates', N'U') IS NOT NULL DROP TABLE [dbo].[UserCollaborationStates];

DECLARE @cleared INT = 0;
IF COL_LENGTH(N'dbo.Contracts', N'LastSelectedAt') IS NOT NULL
BEGIN
    UPDATE [dbo].[Contracts] SET [LastSelectedAt] = NULL WHERE [LastSelectedAt] IS NOT NULL;
    SET @cleared = @@ROWCOUNT;
END

SELECT @deletedDocs AS DeletedDocuments, @cleared AS ClearedSelectionStamps, @deletedBeliefs AS DeletedBeliefs, @deletedDigests AS DeletedDigests, @deletedRevisions AS DeletedRevisions, @deletedInteractions AS DeletedInteractions;
"@

$result = sqlcmd -S $Server -E -d $Database -h -1 -W -Q $query
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE."
}

$nums = @($result | Where-Object { $_ -match '^\d+$' })
$deletedDocs = if ($nums.Count -ge 1) { $nums[0] } else { '0' }
$cleared = if ($nums.Count -ge 2) { $nums[1] } else { '0' }
$deletedBeliefs = if ($nums.Count -ge 3) { $nums[2] } else { '0' }
$deletedDigests = if ($nums.Count -ge 4) { $nums[3] } else { '0' }
$deletedRevisions = if ($nums.Count -ge 5) { $nums[4] } else { '0' }
$deletedInteractions = if ($nums.Count -ge 6) { $nums[5] } else { '0' }

Write-Host "Deleted $deletedDocs belief document(s); $deletedBeliefs belief row(s); $deletedDigests turn digest(s); $deletedRevisions revision(s); $deletedInteractions interaction(s); cleared $cleared contract LastSelectedAt stamp(s)."

if ($IncludeFileState) {
    $dataRoot = Join-Path $PSScriptRoot '..\src\AdaptiveTeamBuilderSvc\data'
    foreach ($sub in 'interactions', 'profiles', 'runs', 'shadow-counters') {
        $dir = Join-Path $dataRoot $sub
        if (Test-Path $dir) {
            Remove-Item -Recurse -Force $dir
            Write-Host "Removed file state: $sub/"
        }
    }
}

Write-Host "Reload Select Contract to see the seeded belief document and the designed DemoSortOrder trio."
