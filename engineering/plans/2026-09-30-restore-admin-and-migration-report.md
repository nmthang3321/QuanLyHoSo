# Restore admin login + migration report — 2026-09-30

Plan: `engineering/plans/2026-09-30-restore-admin-and-migration-plan.md`

## What changed

1. `Infrastructure/Data/AppDataService.cs`
   - `Initialize`'s database-preparation statements were extracted into
     `PrepareDatabase(SqliteConnection connection, bool seedSampleRecords)` —
     identical statement order; sample-record seeding stays conditional.
   - `RestoreDatabaseOnly` now:
     1. Captures the current built-in `admin` credential snapshot
        (`PasswordHash`, `MustChangePassword`) before the swap.
     2. Performs the SQLite backup restore as before.
     3. Runs `PrepareDatabase(destination, seedSampleRecords: false)` — schema
        migrations, seeds, and normalizations, exactly what a fresh server
        start would apply.
     4. Reapplies the admin credential snapshot.
2. Docs: `engineering/features/BACKUP_RESTORE.md` restore bullets updated;
   `engineering/SESSION_HANDOFF.md` 2026-09-30 section extended.
3. Tests — new `tests/QuanLyHoSo.IntegrationTests/RestoreBehaviorTests.cs`:
   - `RestoringOldDatabase_ShouldKeepCurrentAdminPasswordAndRestoredRecords`:
     backup taken before a password change restores with the current password
     still valid, the backup's old password rejected, and the restored record
     present.
   - `RestoringLegacyDatabase_ShouldMigrateSchemaImmediatelyWithoutLosingRecords`:
     a `.db` stripped of the four 2026-09-29 columns restores and
     `GetFilteredRecords` works immediately with no server restart.

## Behavior after the fix

- Restoring any package or legacy `.db` keeps the built-in `admin` password in
  use before the restore; other accounts come from the restored database.
- The restored database is migrated/normalized immediately: restoring an
  older-schema backup no longer breaks queries until the next restart (the
  "no such column: CommanderApproverName" incident class is closed).

## Verification

- Unit tests: 69/69 passed.
- Integration tests: 66/66 passed (64 + 2 new).

## Deployment note

The running server must be rebuilt and restarted once more to pick up this
restore change (the currently running process still has the earlier
duplicate-detection build only).

## Deferred

- `MigrateAccessibleAttachmentsToServerStorage` intentionally runs as part of
  the post-restore sequence (it is part of a normal start); no separate
  attachment handling was added for restore.
