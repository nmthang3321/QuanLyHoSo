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
- Seed demo tao 105 ho so cho 7 can bo (15 ho so/can bo), dung random seed co dinh de du lieu lap lai on dinh.
- `RecordAttachments` co `FilePath` duoc add/check bang `TryAddColumn`.
- `SystemLogs` ghi qua `WriteDatabaseLog`.
- Index tao trong `CreateIndexes(...)`.

Area:
- `Areas` chua xa/phuong/dac khu va don vi to chuc.
- Cac don vi cap tinh/bo/cong an tinh/ngoai tinh seed bang `EnsureStandardOrganizationAreas`.
