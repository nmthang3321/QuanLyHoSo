# Restore keeps admin login and migrates restored data — 2026-09-30

Branch: `main`

## Problem (user requirement)

"khi khôi phục dữ liệu, mật khẩu tài khoản admin vẫn giữ nguyên, dữ liệu cũ
trong bảng nên update mới theo cơ sở dữ liệu mới"

Current restore behavior (`RestoreDatabaseOnly`):

1. `source.BackupDatabase(destination)` replaces the **entire** live database,
   including the `Users` table — the built-in `admin` password reverts to
   whatever the backup contains.
2. **No schema migration or normalization runs after the swap.** Restoring a
   backup made by an older build leaves the live database missing new columns
   (this broke the record list earlier today with
   `no such column: CommanderApproverName` until a server restart).

## Design

- **Admin login is preserved across restore**: before overwriting, capture the
  current `admin` row's `PasswordHash` and `MustChangePassword`; after the
  restore and normalization, write them back onto the restored `admin` row.
  Other accounts keep the backup's values (restore = historical state).
- **The restored database is brought up to the current schema and rules**:
  after the swap, run the same idempotent preparation sequence a fresh server
  start would run (minus sample-record seeding), by extracting `Initialize`'s
  database-preparation steps into a shared `PrepareDatabase(connection,
  seedSampleRecords)` method.
  - This also fixes the restore-under-running-server schema breakage class:
    a restore no longer depends on a later server restart to migrate.

## Changes

1. `Infrastructure/Data/AppDataService.cs`
   - Extract `PrepareDatabase(SqliteConnection connection, bool
     seedSampleRecords)` from `Initialize` (same statement order; sample
     seeding stays conditional).
   - New `NormalizeRestoredDatabase` + `PreserveAdminLogin` helpers.
   - `RestoreDatabaseOnly`: capture the current admin credential snapshot
     before `BackupDatabase`, then run the preparation sequence and reapply the
     snapshot.
2. Tests — new `tests/QuanLyHoSo.IntegrationTests/RestoreBehaviorTests.cs`:
   - Restoring a package taken before a password change keeps the current
     password working and the old one rejected; restored records are present.
   - Restoring a legacy `.db` stripped of the four 2026-09-29 columns migrates
     the schema so `GetFilteredRecords` works immediately, with records and the
     preserved admin password intact.

## Compatibility

- No LAN contract change, no schema change. Packages restored on older builds
  behave as before; on the new build they additionally migrate + preserve admin.
- `SeedUsers` only inserts when `Users` is empty, so the pass never invents
  accounts.

## Verification

- Full non-UI suite green (unit + integration) with the new tests.
- Report: `engineering/plans/2026-09-30-restore-admin-and-migration-report.md`.
