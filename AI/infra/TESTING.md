# Infrastructure - Automated testing

Updated: 2026-09-16

## Purpose and routing

Protect current behavior with priority on data integrity, authorization, and regression. Do not change UI, validation, workflow, or database semantics merely to make testing easier.

- Commands and test-authoring guidance: `AI/infra/TEST_RUNBOOK.md`
- Risk matrix and gaps: `AI/infra/TEST_MATRIX.md`
- Scripts: `tests/Scripts/`
- Generated reports: `tests/Reports/` (gitignored except `.gitkeep`)

## Architecture

- `QuanLyHoSo.UnitTests`: xUnit + Moq for password hashing, role/record authorization, commands, ViewModel notifications/disposal, area selection/filtering, and password-change validation.
- `QuanLyHoSo.IntegrationTests`: xUnit + real isolated SQLite through Core for schema/seeds/auth/users, record lifecycle, Unicode/attachment metadata, search/pagination, authorization/rollback, trash, backup/restore, automatic retention, and document generation.
- `QuanLyHoSo.UITests`: xUnit + FlaUI UIA3 launching the real WPF executable and verifying the login surface.
- Tests target `net8.0`/`net8.0-windows`; production stays on .NET 5.
- Coverlet generates per-run Cobertura coverage; percentages from separate runs are not additive.

## Commands

```powershell
.\tests\Scripts\run-smoke.ps1
.\tests\Scripts\run-unit.ps1
.\tests\Scripts\run-integration.ps1
.\tests\Scripts\run-ui.ps1
.\tests\Scripts\run-regression.ps1
.\tests\Scripts\run-all.ps1
.\tests\Scripts\run-all.ps1 -IncludeUI
```

If execution policy blocks scripts:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Scripts\run-all.ps1
```

- Requires Windows, PowerShell 5.1+, .NET 8 SDK, and .NET 5 Desktop Runtime.
- UI tests need an unlocked interactive session and should not run in headless CI.
- Scripts build first and fail on build/test errors. Do not use `-SkipBuild` when binaries may be stale.
- xUnit categories include Smoke, Unit, Integration, Database, Security, Critical, Regression, Backup, Search, Pagination, Negative, and UI.

## Isolation and production seams

- Each `TestDatabase` uses a unique OS-temp directory and separate DB/logs; cleanup runs even when initialization fails.
- Integration tests disable parallelism because `AuthContext` and `AppPathSettings` are process-global mutable state.
- Internal `AppDataService(string databasePath)` avoids starting the LAN listener and is visible only to integration tests.
- `QUANLYHOSO_TEST_ROOT` redirects Settings/default DB/logs only when explicitly set. UI tests set it only on the child process.
- Login exposes stable AutomationIds without changing visuals.
- Root project excludes `tests/**` from production compile globs.
- Tests use synthetic Vietnamese/Unicode data and temporary credentials only.

## Coverage gaps

- P0: complete processing transitions, forbidden/repeated transitions, and restart during workflow.
- P1: export contents, remaining LAN/auth/disconnect cases, concurrency, file locks/permissions, and authenticated end-to-end UI workflows.
- P2: remaining document-generation failure and file-lock scenarios.

When adding tests, keep them under `tests/`, assert concrete behavior, cover normal/boundary/negative paths, and never use production data. Reproduce confirmed bugs before fixing them. Update `AI/infra/TEST_MATRIX.md` when coverage changes.
