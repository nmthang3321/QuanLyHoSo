# Infrastructure - AppDataService

Use for SQLite query, schema, seed, and data-access tasks. Main file: `Infrastructure\Data\AppDataService.cs`.

Do not read the entire file unless necessary; locate methods first:

```powershell
rg -n "MethodName" Infrastructure\Data\AppDataService.cs -C 5
```

Important areas:
- Schema/init: `Initialize`, `CreateSchema`, `TryAddColumn`, `CreateIndexes`
- Authentication/users: `AuthenticateUser`, `GetUsers`, `SaveUser`, `DeleteUser`
- Catalogs: `GetCatalogValues`, `GetCatalogItems`, catalog CRUD
- Intake: `GetNextRecordCode`, `FindSimilarRecord`, `SaveRecordForm`, `DeleteRecord`
- List/filter/export: `GetFilteredRecords`, `CountFilteredRecords`, `GetExportPreview`, `BuildExportWhere`
- Processing: queue metrics/list/detail and `UpdateProcessingRecord`
- Areas: `GetAreaNames`, `EnsureStandardOrganizationAreas`, `AddOptionalAreaFilter`
- Backup/restore: `BackupDatabase`, `RestoreDatabaseFromFile`
