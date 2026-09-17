# Infrastructure - Database schema

Main tables: `Users`, `Areas`, `CatalogItems`, `Records`, `RecordAttachments`, `ProcessHistories`, and `SystemLogs`.

- `Users.MustChangePassword` forces a password change before entering the shell; migration uses `TryAddColumn` with legacy default `0`.
- `CatalogItems` contains `CatalogType`, `Name`, `DisplayOrder`, and `IsActive`.
- Normal production initialization creates no records. The explicit sample-data flow creates 105 demo records for seven officers using a fixed random seed.
- `RecordAttachments.FilePath` is added/checked through `TryAddColumn`.
- `SystemLogs` entries are written through `WriteDatabaseLog`.
- Indexes are created in `CreateIndexes(...)`.
- Soft delete uses `Records.DeletedAt`, `DeletedBy`, and `DeletionBatchId`; see `engineering/features/RECORD_TRASH.md`.
- `Areas` contains communes/wards/special zones and organization units. Standard organizations are seeded by `EnsureStandardOrganizationAreas`.
