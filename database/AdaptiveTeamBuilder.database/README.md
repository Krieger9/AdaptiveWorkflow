# AdaptiveTeamBuilder.database

SQL Server database project (`Microsoft.Build.Sql`). This is the **schema source of truth**.

EF Core in `AdaptiveTeamBuilder.Data` maps to these objects for read/write only — it does **not** own or migrate schema.

## Local publish (recommended)

After schema changes (or on first setup), publish the DACPAC to LocalDB:

```powershell
.\database\publish-local.ps1
```

Defaults: `(localdb)\MSSQLLocalDB` / `AdaptiveTeamBuilder` with Windows integrated auth.

### When to run it

| Situation | Run publish? |
| --- | --- |
| First clone / empty local DB | Yes |
| Changed `.sql` under `database/` | Yes, before running the API |
| Only C# / React code changed | No |
| Every API start | No — keep deploy explicit |

Optional convenience: run `publish-local.ps1` at the start of your local day or from a personal `start-dev.ps1`, but keep it out of the API process itself.

## Build only

```bash
dotnet build database/AdaptiveTeamBuilder.database/AdaptiveTeamBuilder.database.sqlproj
```

## Manual SqlPackage

```bash
dotnet tool install -g microsoft.sqlpackage

sqlpackage /Action:Publish ^
  /SourceFile:database/AdaptiveTeamBuilder.database/bin/Debug/AdaptiveTeamBuilder.database.dacpac ^
  /TargetServerName:(localdb)\MSSQLLocalDB ^
  /TargetDatabaseName:AdaptiveTeamBuilder ^
  /TargetTrustServerCertificate:True
```
