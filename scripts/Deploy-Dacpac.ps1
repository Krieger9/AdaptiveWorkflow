<#
.SYNOPSIS
	Deploys the AdaptiveTeamBuilder database DACPAC to a target SQL Server.

.DESCRIPTION
	Builds (optionally) and publishes the DACPAC produced by the
	AdaptiveTeamBuilder.database project to the specified SQL Server instance
	using SqlPackage. By default it targets (localdb)\MSSQLLocalDB and the
	'AdaptiveTeamBuilder' database.

	The -AllowDataLoss switch (enabled by default) sets
	BlockOnPossibleDataLoss=False so the deployment will proceed even when
	schema changes could drop data.

.PARAMETER ServerName
	The target SQL Server instance. Defaults to '(localdb)\MSSQLLocalDB'.

.PARAMETER DatabaseName
	The target database name. Defaults to 'AdaptiveTeamBuilder'.

.PARAMETER DacpacPath
	Path to the .dacpac file. Defaults to the project's build output.

.PARAMETER SkipBuild
	When specified, skips building the database project and deploys the
	existing DACPAC. By default the project is built before deploying.

.PARAMETER Configuration
	Build configuration used to locate/build the DACPAC. Defaults to 'Debug'.

.PARAMETER AllowDataLoss
	When $true (default), sets BlockOnPossibleDataLoss=False so the deployment
	does not fail on potential data loss.

.EXAMPLE
	./scripts/Deploy-Dacpac.ps1

.EXAMPLE
	./scripts/Deploy-Dacpac.ps1 -SkipBuild

.EXAMPLE
	./scripts/Deploy-Dacpac.ps1 -ServerName '(localdb)\MSSQLLocalDB' -DatabaseName 'AdaptiveTeamBuilder' -AllowDataLoss:$false
#>
[CmdletBinding()]
param(
	[string]$ServerName = '(localdb)\MSSQLLocalDB',
	[string]$DatabaseName = 'AdaptiveTeamBuilder',
	[string]$DacpacPath,
	[switch]$SkipBuild,
	[string]$Configuration = 'Debug',
	[bool]$AllowDataLoss = $true
)

$ErrorActionPreference = 'Stop'

# Resolve repository-relative paths (script lives in ./scripts).
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectDir = Join-Path $RepoRoot 'database\AdaptiveTeamBuilder.database'
$ProjectFile = Join-Path $ProjectDir 'AdaptiveTeamBuilder.database.sqlproj'

if (-not $DacpacPath) {
	$DacpacPath = Join-Path $ProjectDir "bin\$Configuration\AdaptiveTeamBuilder.database.dacpac"
}

# Build the database project to produce a fresh DACPAC (default behavior).
if (-not $SkipBuild) {
	Write-Host "Building database project ($Configuration)..." -ForegroundColor Cyan
	dotnet build $ProjectFile -c $Configuration
	if ($LASTEXITCODE -ne 0) {
		throw "Build failed with exit code $LASTEXITCODE."
	}
}

if (-not (Test-Path $DacpacPath)) {
	throw "DACPAC not found at '$DacpacPath'. Build the project or specify -DacpacPath."
}

# Ensure SqlPackage is available.
$sqlPackage = Get-Command 'SqlPackage' -ErrorAction SilentlyContinue
if (-not $sqlPackage) {
	$sqlPackage = Get-Command 'sqlpackage' -ErrorAction SilentlyContinue
}
if (-not $sqlPackage) {
	throw "SqlPackage was not found on PATH. Install it with: dotnet tool install -g microsoft.sqlpackage"
}

$targetConnection = "Server=$ServerName;Database=$DatabaseName;Integrated Security=true;TrustServerCertificate=true;"

# SqlPackage expects lowercase 'true'/'false' string values.
$blockOnDataLoss = (-not $AllowDataLoss).ToString().ToLower()

Write-Host "Deploying DACPAC..." -ForegroundColor Cyan
Write-Host "  Source   : $DacpacPath"
Write-Host "  Server   : $ServerName"
Write-Host "  Database : $DatabaseName"
Write-Host "  BlockOnPossibleDataLoss=$blockOnDataLoss"

& $sqlPackage.Source `
	/Action:Publish `
	/SourceFile:"$DacpacPath" `
	/TargetConnectionString:"$targetConnection" `
	/p:BlockOnPossibleDataLoss=$blockOnDataLoss `
	/p:DropObjectsNotInSource=false

if ($LASTEXITCODE -ne 0) {
	throw "Deployment failed with exit code $LASTEXITCODE."
}

Write-Host "Deployment completed successfully." -ForegroundColor Green
