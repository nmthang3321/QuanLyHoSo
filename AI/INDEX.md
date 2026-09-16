# AI index - QuanLyHoSo

All AI/developer documentation is consolidated under `AI/`. Start with `AI/AI_INSTRUCTIONS.md`, `AI/SESSION_HANDOFF.md`, and `AI/RULES.md`, then use this index to open only task-relevant references.

Updated: 2026-09-16

Read this file first, then only the files relevant to the current task. The session handoff is `AI/SESSION_HANDOFF.md`.

## High-level routing

- AI working instructions: `AI/AI_INSTRUCTIONS.md`
- Developer guide: `AI/DEVELOPER_GUIDE.md`
- Current architecture: `AI/ARCHITECTURE.md`
- Coding guidelines: `AI/CODING_GUIDELINES.md`
- Refactoring decisions and technical debt: `AI/REFACTORING_NOTES.md`
- Repository overview, verification, and roles: `AI/COMMON.md`
- LAN/client/server: `AI/LAN.md`
- Dashboard: `AI/DASHBOARD.md`
- Record intake: `AI/RECORD_INPUT.md`
- Record list: `AI/RECORD_LIST.md`
- Classification and processing: `AI/PROCESSING.md`
- Staff tracking: `AI/STAFF_TRACKING.md`
- Settings: `AI/SETTINGS.md`
- Database/schema: `AI/DATABASE.md`
- Coding/build/git rules: `AI/RULES.md`

## Pages

- Login: `AI/pages/LOGIN.md`
- Dashboard: `AI/pages/DASHBOARD_OVERVIEW.md`
- Record intake: `AI/pages/RECORD_INPUT_FORM.md`
- Record-list filters/table: `AI/pages/RECORD_LIST_FILTERS.md`, `AI/pages/RECORD_LIST_TABLE.md`
- Processing queue/detail: `AI/pages/PROCESSING_QUEUE.md`, `AI/pages/PROCESSING_DETAIL.md`
- Staff tracking: `AI/pages/STAFF_TRACKING.md`
- Settings home: `AI/pages/SETTINGS_HOME.md`

## Popups and overlays

- Dashboard date range: `AI/popups/DASHBOARD_DATE_RANGE.md`
- Record details: `AI/popups/RECORD_DETAIL_MODAL.md`
- Settings catalog: `AI/popups/SETTINGS_CATALOG_DIALOG.md`
- Removed legacy general settings: `AI/popups/SETTINGS_GENERAL_DIALOG.md`
- Settings guide: `AI/popups/SETTINGS_GUIDE_DIALOG.md`
- System log: `AI/popups/SETTINGS_SYSTEM_LOG_DIALOG.md`
- User management: `AI/popups/SETTINGS_USER_DIALOG.md`

## Features

- Area selector: `AI/features/AREA_SELECTOR.md`
- Attachments: `AI/features/ATTACHMENTS.md`
- Resubmitted records/sender history: `AI/features/RECORD_RESUBMISSION.md`
- Record trash/restore: `AI/features/RECORD_TRASH.md`
- Audit log: `AI/features/AUDIT_LOG.md`
- Backup/restore: `AI/features/BACKUP_RESTORE.md`
- Catalogs: `AI/features/CATALOGS.md`
- Excel export: `AI/features/EXPORT_EXCEL.md`
- Navigation: `AI/features/NAVIGATION.md`

## Infrastructure

- Build/git: `AI/infra/BUILD_GIT.md`
- Tests and coverage gaps: `AI/infra/TESTING.md`
- Test execution runbook: `AI/infra/TEST_RUNBOOK.md`
- Risk-based test matrix: `AI/infra/TEST_MATRIX.md`
- Sample database: `AI/infra/SAMPLE_DATA.md`
- Data-service routing: `AI/infra/DATA_SERVICE.md`
- Database schema: `AI/infra/DATABASE_SCHEMA.md`
- LAN API: `AI/infra/LAN_API.md`

## Current reminders

- Automated tests live under `tests/`. Run smoke after code changes and the full noninteractive suite before merge/release.
- Read `AI/features/AREA_SELECTOR.md` before changing area UI.
- Build to an isolated output directory to avoid locks from running executables:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

- `NETSDK1138` about unsupported `.NET 5.0-windows` is an existing warning; a build succeeds when it has zero errors.
