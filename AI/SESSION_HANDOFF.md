# Session handoff - QuanLyHoSo

Last updated: 2026-09-16

This file is the compact starting context for future AI sessions. For detailed behavior, follow the links in `AI/INDEX.md` instead of loading every document.

## Start here

1. Read `AI/INDEX.md` and `AI/RULES.md`.
2. Run `git status --short --branch`.
3. Read only the page, popup, feature, or infrastructure document relevant to the current task.

Documentation routing:

- Small pages: `AI/pages/*.md`
- Popups and overlays: `AI/popups/*.md`
- Shared features: `AI/features/*.md`
- Database, LAN, build, and testing: `AI/infra/*.md`
- Automated-test inventory: `AI/infra/TEST_MATRIX.md`

## Documentation language policy

- All AI/developer-facing files are consolidated under `AI/` and written in English.
- The root `README.md` and customer-facing documents under `doc/` remain in Vietnamese unless the user explicitly requests otherwise.
- Vietnamese UI labels, business statuses, database values, and exact user-visible messages remain unchanged when referenced in English technical documentation.

## Current uncommitted documentation-template change

- `phieu_de_xuat.docx`, `phieu_huong_dan.docx`, and `thong_bao.docx` were moved from `doc/` to `doc/templates/`.
- The client, server, integration tests, project copy rules, and `InitialResultDocumentGenerator` now use `doc/templates/`.
- Output filenames and document-generation behavior are unchanged.
- Verification completed before the documentation translation: client/server builds passed, five document tests passed, and the smoke suite passed 6/6.

## Automatic backup and Settings layout - 2026-09-16

- The server checks automatic backup on startup and once per hour while running. It creates a new SQLite online backup when the latest automatic backup is at least seven days old.
- Automatic filenames use `quanlyhoso_auto_yyyyMMdd_HHmmss.db` under `%LocalAppData%\QuanLyHoSo\Backup` on the server.
- Cleanup retains the ten newest `quanlyhoso_*.db` files across automatic, manual, legacy, and pre-restore safety backups. Cleanup runs on each automatic check.
- The Data Backup card is split into automatic-backup status, manual backup, and restore sections. Long paths use ellipsis and tooltips; restore uses a mild warning treatment.
- In the Admin layout, Data Backup is to the left of Quick Actions. Quick Actions is top-aligned and keeps its content height instead of stretching to match the backup card.
- Restore uploads use a temporary `.restore_upload_*.db` file, which is deleted after the restore attempt finishes.

See `AI/features/BACKUP_RESTORE.md` and `AI/pages/SETTINGS_HOME.md`.

## Initial-result transfer documents - 2026-09-14

- Confirming document creation opens an overlay for transfer number, transfer date, and complaint-forwarding date. All three values are required; cancel keeps the form, success closes the overlay, and errors keep it open.
- The read-only preview includes sender name, receive source, content summary, note, proposal, and contact address. A newly entered `ProcessingNote` takes precedence over the stored note.
- `InitialResultDocumentDetails` travels through the service/LAN boundary to the generator. Client and server must therefore be updated together; no schema change was introduced.
- The extra values are used only for the current document-generation operation and are not stored as record fields.
- The three Word templates preserve their existing runs/styles and map the highlighted regions to the new values.

## Processing and form behavior - 2026-09-14

- The seven workflow icons have Vietnamese guidance tooltips. Step 6 is displayed as `Chờ kết quả`; stored history keys and status mappings were not changed.
- Destination agency is required when forwarding to another agency.
- The record-input action bar is fixed below the scrollable content and is disabled while the comparison overlay is open.
- Every field in General Information is required, including phone number, contact address, incident address, and generated record code. Whitespace-only values are rejected.
- Leaders can open processing details in read-only mode but cannot save, delete, or otherwise update processing.
- All roles land on Dashboard after sign-in, including after mandatory password changes.
- Processing details now fall back to the record-form attachment list when the processing payload contains no attachments, preventing persisted files from being shown as missing. A ViewModel regression test covers this case.

See `AI/pages/PROCESSING_DETAIL.md`, `AI/pages/RECORD_INPUT_FORM.md`, and `AI/features/NAVIGATION.md`.

## Tables, metrics, and selectable text - 2026-09-14

- Sidebar navigation caches page ViewModels for the signed-in session, so revisiting a page no longer repeats constructor-time LAN calls. List, processing, and staff refreshes run in the background; independent first-visit catalog calls run concurrently. Input and Processing direct-navigation cleanup behavior is preserved.
- Dashboard LAN loading no longer stacks six independent round trips sequentially. The requests start concurrently, and an explicit loading surface covers the initially empty charts until the snapshot is ready; a regression test verifies both behaviors.
- Record-list text is selectable and copyable. Full-row selection remains visually distinct; multi-record checkboxes continue to use their own data selection state.
- Double-clicking text selects the full cell text. Dashboard task cards preserve text selection while empty card space remains clickable.
- Shared metric info icons explain dashboard, processing, and staff-tracking calculations in Vietnamese tooltips.
- Staff KPI and status use the current on-time-rate formula and the 90%/80% thresholds. The query logic was not changed.

## Resubmitted records - 2026-09-13

- New input compares unlimited sender history. The officer may save an independent record or link a resubmission to an already resolved original record; a reason is mandatory for resubmission.
- A resubmission keeps its own code, receive date, content, and attachments; it uses status `Đã giải quyết — hồ sơ gửi lại` and links to the original record.
- Resubmissions do not enter the pending queue, inflate resolved/KPI counts, or generate fake verification history.
- The server determines normalized sender identity. A sender name alone is not sufficient identification, and sender-merging is not implemented.
- Schema changes consist of three metadata columns and two indexes with idempotent migration. Client and server must be upgraded together.
- The server protects links when editing, deleting, changing record code/sender/incident, or reopening an original record.
- The sender-history comparison grid no longer leaves a blank area resembling an extra column. Its grid lines, cell spacing, selected-row colors, and secondary/primary actions now match the Record List and shared application styles; the selected row remains highlighted while the application is inactive. Shared button templates apply their declared horizontal padding. Primary buttons always render white text and are not visually faded when disabled.

See `AI/features/RECORD_RESUBMISSION.md`.

## Customer documentation

- `doc/Huong_dan_su_dung_QuanLyHoSo.md` was refreshed on 2026-09-16 to use the current Admin, Leader, Officer, and Server screenshots.
- The user guide now documents sender-history comparison/resubmission, the initial-result document preview, automatic seven-day backups with ten-file retention, and restore safety/temp-file behavior.

## Architecture snapshot

- The solution is split into `QuanLyHoSo.Shared`, `QuanLyHoSo.Core`, the WPF client, and `QuanLyHoSo.Server`.
- The server owns SQLite, logs, backup/restore, and the LAN API. The WPF application normally runs in Client mode; only explicit `AdminHost` mode uses a local database.
- The server has a compact WPF administration/tray UI. Closing or minimizing hides it to the tray; only the explicit exit action stops the server.
- The client sends a 30-second heartbeat. The server considers unique machine names active for 90 seconds.
- The server can start/stop LAN listening, reset the built-in `admin`, open data/log folders, and copy the client URL.
- Attachment operations over LAN currently store metadata/path only; physical client-to-server file upload is not implemented.
- Technical log files matching `quanlyhoso-yyyyMMdd.log` are retained for 30 days.

## Verification baseline

- Latest noninteractive verification on 2026-09-16: `tests/Scripts/run-all.ps1` passed 83 tests (43 unit/ViewModel and 40 integration); Release build completed with zero errors. UI tests were not included in this run.
- Historical full verification on 2026-09-14: Release solution build and `tests/Scripts/run-all.ps1 -IncludeUI` passed 80 tests (40 unit, 39 integration, 1 UI smoke).
- Test projects target .NET 8 while the application remains on .NET 5.
- Each integration test uses an isolated temporary database. Generated test reports are ignored by Git.
- Existing tests do not constitute full feature coverage. Gaps include full authenticated UI workflows, all processing transitions, LAN failures/concurrency, export contents, and file-lock/permission failures.
- Re-run verification after code changes; historical results are context, not proof for new work.
