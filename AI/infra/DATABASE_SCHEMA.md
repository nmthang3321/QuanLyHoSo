# Infra - Database schema

Bang chinh:
- `Users`
- `Areas`
- `CatalogItems`
- `Records`
- `RecordAttachments`
- `ProcessHistories`
- `SystemLogs`

Notes:
- `CatalogItems` co `CatalogType`, `Name`, `DisplayOrder`, `IsActive`.
- `RecordAttachments` co `FilePath` duoc add/check bang `TryAddColumn`.
- `SystemLogs` ghi qua `WriteDatabaseLog`.
- Index tao trong `CreateIndexes(...)`.

Area:
- `Areas` chua xa/phuong/dac khu va don vi to chuc.
- Cac don vi cap tinh/bo/cong an tinh/ngoai tinh seed bang `EnsureStandardOrganizationAreas`.

