# Infrastructure - Database schema

Main tables: `Users`, `Areas`, `CatalogItems`, `Records`, `RecordAttachments`, `ProcessHistories`, and `SystemLogs`.

- `Users.MustChangePassword` forces a password change before entering the shell; migration uses `TryAddColumn` with legacy default `0`.
- `CatalogItems` contains `CatalogType`, `Name`, `DisplayOrder`, and `IsActive`.
- Demo seed creates 105 records for seven officers using a fixed random seed.
- `RecordAttachments.FilePath` is added/checked through `TryAddColumn`.
- `SystemLogs` entries are written through `WriteDatabaseLog`.
- Indexes are created in `CreateIndexes(...)`.
- Soft delete uses `Records.DeletedAt`, `DeletedBy`, and `DeletionBatchId`; see `AI/features/RECORD_TRASH.md`.
- `Areas` contains communes/wards/special zones and organization units. Standard organizations are seeded by `EnsureStandardOrganizationAreas`.
