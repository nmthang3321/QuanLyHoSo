# Infra - AppDataService

Dung khi task lien quan query SQLite/schema/seed/data access.

File:
- `Infrastructure\Data\AppDataService.cs`

Rules:
- Khong doc ca file neu khong can; tim method bang:

```powershell
rg -n "MethodName" Infrastructure\Data\AppDataService.cs -C 5
```

Important methods/areas:
- schema/init: `Initialize`, `CreateSchema`, `TryAddColumn`, `CreateIndexes`
- auth/users: `AuthenticateUser`, `GetUsers`, `SaveUser`, `DeleteUser`
- catalogs: `GetCatalogValues`, `GetCatalogItems`, `AddCatalogItem`, `UpdateCatalogItem`, `DeleteCatalogItem`
- record input: `GetNextRecordCode`, `FindSimilarRecord`, `SaveRecordForm`, `DeleteRecord`
- list/filter/export: `GetFilteredRecords`, `CountFilteredRecords`, `GetExportPreview`, `BuildExportWhere`
- processing: `GetProcessingQueueMetrics`, `GetProcessingQueueRecords`, `GetProcessingRecordDetail`, `UpdateProcessingRecord`
- area: `GetAreaNames`, `EnsureStandardOrganizationAreas`, `AddOptionalAreaFilter`
- backup: `BackupDatabase`, `RestoreDatabaseFromFile`

