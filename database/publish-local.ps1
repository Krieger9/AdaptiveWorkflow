<#
.SYNOPSIS
  Builds AdaptiveTeamBuilder.database and publishes the DACPAC to local LocalDB.

.DESCRIPTION
  Schema source of truth is the .sqlproj. Run this after changing SQL objects,
  and before starting the API when the local database may be out of date.

  Recommended local workflow:
  - After cloning / first setup: run once
  - After editing anything under database/: run again
  - Do NOT run on every API request or as a silent startup side effect

.PARAMETER Server
  SQL Server instance. Default: (localdb)\MSSQLLocalDB

.PARAMETER Database
  Target database name. Default: AdaptiveTeamBuilder
#>
[CmdletBinding()]
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'AdaptiveTeamBuilder'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$sqlProj = Join-Path $repoRoot 'database\AdaptiveTeamBuilder.database\AdaptiveTeamBuilder.database.sqlproj'
$dacpac = Join-Path $repoRoot 'database\AdaptiveTeamBuilder.database\bin\Debug\AdaptiveTeamBuilder.database.dacpac'

Write-Host "Building database project..."
dotnet build $sqlProj -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "Database project build failed."
}

if (-not (Test-Path $dacpac)) {
    throw "DACPAC not found at $dacpac"
}

$sqlpackage = Get-Command sqlpackage -ErrorAction SilentlyContinue
if (-not $sqlpackage) {
    Write-Host "sqlpackage not found on PATH. Installing microsoft.sqlpackage global tool..."
    dotnet tool install -g microsoft.sqlpackage
    $env:Path = [System.Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
                [System.Environment]::GetEnvironmentVariable('Path', 'User')
    $sqlpackage = Get-Command sqlpackage -ErrorAction Stop
}

Write-Host "Publishing DACPAC to $Server / $Database ..."

# Clear POC tables that may block breaking schema updates, then republish + reseed.
sqlcmd -S $Server -E -Q @"
IF DB_ID(N'$Database') IS NOT NULL
BEGIN
    USE [$Database];
    IF OBJECT_ID(N'dbo.TeamHiddenProfiles', N'U') IS NOT NULL DROP TABLE [dbo].[TeamHiddenProfiles];
    IF OBJECT_ID(N'dbo.TeamMembers', N'U') IS NOT NULL DROP TABLE [dbo].[TeamMembers];
    IF OBJECT_ID(N'dbo.TeamPositionRequirements', N'U') IS NOT NULL DROP TABLE [dbo].[TeamPositionRequirements];
    IF OBJECT_ID(N'dbo.Teams', N'U') IS NOT NULL DROP TABLE [dbo].[Teams];
    IF OBJECT_ID(N'dbo.ContractMilestones', N'U') IS NOT NULL DROP TABLE [dbo].[ContractMilestones];
    IF OBJECT_ID(N'dbo.ContractDeliverables', N'U') IS NOT NULL DROP TABLE [dbo].[ContractDeliverables];
    IF OBJECT_ID(N'dbo.ContractConstraints', N'U') IS NOT NULL DROP TABLE [dbo].[ContractConstraints];
    IF OBJECT_ID(N'dbo.ContractSkills', N'U') IS NOT NULL DROP TABLE [dbo].[ContractSkills];
    IF OBJECT_ID(N'dbo.Contracts', N'U') IS NOT NULL DROP TABLE [dbo].[Contracts];
    IF OBJECT_ID(N'dbo.ContractConstraintTypes', N'U') IS NOT NULL DROP TABLE [dbo].[ContractConstraintTypes];
    IF OBJECT_ID(N'dbo.ContractSkillPriorities', N'U') IS NOT NULL DROP TABLE [dbo].[ContractSkillPriorities];
    IF OBJECT_ID(N'dbo.ContractEngagementTypes', N'U') IS NOT NULL DROP TABLE [dbo].[ContractEngagementTypes];
    IF OBJECT_ID(N'dbo.ContractWorkModes', N'U') IS NOT NULL DROP TABLE [dbo].[ContractWorkModes];
    IF OBJECT_ID(N'dbo.ContractDeliveryRiskLevels', N'U') IS NOT NULL DROP TABLE [dbo].[ContractDeliveryRiskLevels];
    IF OBJECT_ID(N'dbo.ContractStrategicValueLevels', N'U') IS NOT NULL DROP TABLE [dbo].[ContractStrategicValueLevels];
    IF OBJECT_ID(N'dbo.EmployeeProfileSkills', N'U') IS NOT NULL DROP TABLE [dbo].[EmployeeProfileSkills];
    IF OBJECT_ID(N'dbo.EmployeeProfiles', N'U') IS NOT NULL DROP TABLE [dbo].[EmployeeProfiles];
    IF OBJECT_ID(N'dbo.RoleSpecialties', N'U') IS NOT NULL DROP TABLE [dbo].[RoleSpecialties];
    IF OBJECT_ID(N'dbo.ExperienceLevels', N'U') IS NOT NULL DROP TABLE [dbo].[ExperienceLevels];
    IF OBJECT_ID(N'dbo.PositionTypes', N'U') IS NOT NULL DROP TABLE [dbo].[PositionTypes];
    IF OBJECT_ID(N'dbo.Skills', N'U') IS NOT NULL DROP TABLE [dbo].[Skills];
    IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DELETE FROM [dbo].[Users];
END
"@ | Out-Null

# DropObjectsNotInSource removes anything not defined in the .sqlproj (legacy tables,
# __EFMigrationsHistory, etc.) so the target always matches the project exactly.
# Security objects are excluded so logins/users/permissions survive the publish.
& sqlpackage `
    /Action:Publish `
    /SourceFile:$dacpac `
    /TargetServerName:$Server `
    /TargetDatabaseName:$Database `
    /TargetTrustServerCertificate:True `
    /p:BlockOnPossibleDataLoss=false `
    /p:DropObjectsNotInSource=true `
    /p:DoNotDropObjectTypes='Logins;Users;Permissions;RoleMembership'

if ($LASTEXITCODE -ne 0) {
    throw "sqlpackage publish failed with exit code $LASTEXITCODE."
}

Write-Host "Local database schema is up to date."
