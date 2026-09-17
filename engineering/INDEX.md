# Engineering index - QuanLyHoSo

All internal engineering documentation is consolidated under `engineering/`. Start with `engineering/MAINTENANCE_GUIDE.md`, `engineering/SESSION_HANDOFF.md`, and `engineering/RULES.md`, then use this index to open only task-relevant references.

Updated: 2026-09-16

Read this file first, then only the files relevant to the current task. The session handoff is `engineering/SESSION_HANDOFF.md`.

## High-level routing

- Maintenance working instructions: `engineering/MAINTENANCE_GUIDE.md`
- Developer guide: `engineering/DEVELOPER_GUIDE.md`
- Current architecture: `engineering/ARCHITECTURE.md`
- Coding guidelines: `engineering/CODING_GUIDELINES.md`
- Refactoring decisions and technical debt: `engineering/REFACTORING_NOTES.md`
- Repository overview, verification, and roles: `engineering/COMMON.md`
- LAN/client/server: `engineering/LAN.md`
- Dashboard: `engineering/DASHBOARD.md`
- Record intake: `engineering/RECORD_INPUT.md`
- Record list: `engineering/RECORD_LIST.md`
- Classification and processing: `engineering/PROCESSING.md`
- Staff tracking: `engineering/STAFF_TRACKING.md`
- Settings: `engineering/SETTINGS.md`
- Database/schema: `engineering/DATABASE.md`
- Coding/build/git rules: `engineering/RULES.md`

## Pages

- Login: `engineering/pages/LOGIN.md`
- Dashboard: `engineering/pages/DASHBOARD_OVERVIEW.md`
- Record intake: `engineering/pages/RECORD_INPUT_FORM.md`
- Record-list filters/table: `engineering/pages/RECORD_LIST_FILTERS.md`, `engineering/pages/RECORD_LIST_TABLE.md`
- Processing queue/detail: `engineering/pages/PROCESSING_QUEUE.md`, `engineering/pages/PROCESSING_DETAIL.md`
- Staff tracking: `engineering/pages/STAFF_TRACKING.md`
- Settings home: `engineering/pages/SETTINGS_HOME.md`

## Popups and overlays

- Dashboard date range: `engineering/popups/DASHBOARD_DATE_RANGE.md`
- Record details: `engineering/popups/RECORD_DETAIL_MODAL.md`
- Settings catalog: `engineering/popups/SETTINGS_CATALOG_DIALOG.md`
- Removed legacy general settings: `engineering/popups/SETTINGS_GENERAL_DIALOG.md`
- Settings guide: `engineering/popups/SETTINGS_GUIDE_DIALOG.md`
- System log: `engineering/popups/SETTINGS_SYSTEM_LOG_DIALOG.md`
- User management: `engineering/popups/SETTINGS_USER_DIALOG.md`

## Features

- Area selector: `engineering/features/AREA_SELECTOR.md`
- Attachments: `engineering/features/ATTACHMENTS.md`
- Resubmitted records/sender history: `engineering/features/RECORD_RESUBMISSION.md`
- Record trash/restore: `engineering/features/RECORD_TRASH.md`
- Audit log: `engineering/features/AUDIT_LOG.md`
- Backup/restore: `engineering/features/BACKUP_RESTORE.md`
- Catalogs: `engineering/features/CATALOGS.md`
- Excel export: `engineering/features/EXPORT_EXCEL.md`
- Navigation: `engineering/features/NAVIGATION.md`

## Infrastructure

- Customer PDF generation and visual QA: `engineering/infra/CUSTOMER_PDF.md`
- Build/git: `engineering/infra/BUILD_GIT.md`
- Tests and coverage gaps: `engineering/infra/TESTING.md`
- Test execution runbook: `engineering/infra/TEST_RUNBOOK.md`
- Risk-based test matrix: `engineering/infra/TEST_MATRIX.md`
- Sample database: `engineering/infra/SAMPLE_DATA.md`
- Data-service routing: `engineering/infra/DATA_SERVICE.md`
- Database schema: `engineering/infra/DATABASE_SCHEMA.md`
- LAN API: `engineering/infra/LAN_API.md`

## Current reminders

- Automated tests live under `tests/`. Run smoke after code changes and the full noninteractive suite before merge/release.
- Read `engineering/features/AREA_SELECTOR.md` before changing area UI.
- Build to an isolated output directory to avoid locks from running executables:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

- `NETSDK1138` about unsupported `.NET 5.0-windows` is an existing warning; a build succeeds when it has zero errors.
