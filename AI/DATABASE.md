# Database and schema

Detailed references: `AI/infra/DATABASE_SCHEMA.md` and `AI/infra/DATA_SERVICE.md`.

Main tables: `Users`, `Areas`, `CatalogItems`, `Records`, `RecordAttachments`, `ProcessHistories`, and `SystemLogs`.

Notes:
- The `ProcessorName` catalog is synchronized from `Records.ProcessorName`.
- `TryAddColumn` checks `PRAGMA table_info` before `ALTER TABLE`, avoiding repeated `duplicate column name: FilePath` warnings.
- For a task involving one query, do not read all of `AppDataService.cs`; locate the relevant method with `rg`.

Areas:
- `EnsureStandardOrganizationAreas(connection)` seeds province, ministry, provincial-police, and out-of-province organizations.
- `AddOptionalAreaFilter()` handles group filters.
- `Cấp xã` maps by `AreaType` for communes, wards, and special zones.
- Other groups map by `AreaType` or a defined set of `AreaName` values.
