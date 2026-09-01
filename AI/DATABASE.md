# Database/schema

Chi tiet hon:
- `AI/infra/DATABASE_SCHEMA.md`
- `AI/infra/DATA_SERVICE.md`

Bang chinh:
- `Users`
- `Areas`
- `CatalogItems`
- `Records`
- `RecordAttachments`
- `ProcessHistories`
- `SystemLogs`

Ghi chu:
- Catalog `ProcessorName` duoc sync tu `Records.ProcessorName`.
- `TryAddColumn` da doi sang check `PRAGMA table_info` truoc khi `ALTER TABLE`, de khong con warning lap lai `duplicate column name: FilePath`.
- Neu task chi lien quan 1 query, khong doc ca `AppDataService.cs`; tim method bang `rg`.

Dia ban:
- `EnsureStandardOrganizationAreas(connection)` seed cac don vi cap tinh/bo/cong an tinh/ngoai tinh.
- `AddOptionalAreaFilter()` xu ly group filter.
- `Cap xa` map theo `AreaType` xa/phuong/dac khu.
- Group khac map theo `AreaType` hoac tap `AreaName`.
