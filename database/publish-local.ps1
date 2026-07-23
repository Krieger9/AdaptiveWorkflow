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
    IF OBJECT_ID(N'dbo.EmployeeProfileSkills', N'U') IS NOT NULL DROP TABLE [dbo].[EmployeeProfileSkills];
    IF OBJECT_ID(N'dbo.EmployeeProfiles', N'U') IS NOT NULL DROP TABLE [dbo].[EmployeeProfiles];
    IF OBJECT_ID(N'dbo.RoleSpecialties', N'U') IS NOT NULL DROP TABLE [dbo].[RoleSpecialties];
    IF OBJECT_ID(N'dbo.ExperienceLevels', N'U') IS NOT NULL DROP TABLE [dbo].[ExperienceLevels];
    IF OBJECT_ID(N'dbo.PositionTypes', N'U') IS NOT NULL DROP TABLE [dbo].[PositionTypes];
    IF OBJECT_ID(N'dbo.Skills', N'U') IS NOT NULL DROP TABLE [dbo].[Skills];
    IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DELETE FROM [dbo].[Users];
END
"@ | Out-Null

& sqlpackage `
    /Action:Publish `
    /SourceFile:$dacpac `
    /TargetServerName:$Server `
    /TargetDatabaseName:$Database `
    /TargetTrustServerCertificate:True `
    /p:BlockOnPossibleDataLoss=false

if ($LASTEXITCODE -ne 0) {
    throw "sqlpackage publish failed with exit code $LASTEXITCODE."
}

# Remove leftover EF migration history if a prior migration-based bootstrap created it.
Write-Host "Dropping legacy __EFMigrationsHistory (if present)..."
sqlcmd -S $Server -E -d $Database -Q "IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL DROP TABLE [dbo].[__EFMigrationsHistory];" | Out-Null

Write-Host "Local database schema is up to date."
