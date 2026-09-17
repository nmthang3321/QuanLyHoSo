# Automated tests

The suite protects the current behavior without using production data. Tests are split into fast unit/ViewModel checks, real SQLite integration tests, and a small Windows UI smoke layer.

## Projects

- `QuanLyHoSo.UnitTests`: security helpers, authorization rules, commands, notifications, selection/search helpers, and password-change ViewModel validation.
- `QuanLyHoSo.IntegrationTests`: fresh-database initialization, authentication, users, record CRUD, Unicode persistence, search, pagination, authorization, trash/restore, transaction rollback, backup, and restore.
- `QuanLyHoSo.UITests`: launches the real WPF executable and verifies the login window through Windows UI Automation.
- `Scripts`: repeatable build/test/report entry points.
- `Reports`: generated TRX and coverage output (ignored except for the directory marker).

## Prerequisites

- Windows 10/11 with the .NET 8 SDK plus the .NET 5 Desktop Runtime used by the application.
- An interactive desktop session for UI tests. Do not run UI tests in a locked session.
- PowerShell 5.1 or newer.

## Commands

```powershell
.\tests\Scripts\run-smoke.ps1
.\tests\Scripts\run-unit.ps1
.\tests\Scripts\run-integration.ps1
.\tests\Scripts\run-regression.ps1
.\tests\Scripts\run-ui.ps1
.\tests\Scripts\run-all.ps1
.\tests\Scripts\run-all.ps1 -IncludeUI
```

`run-all.ps1` runs all noninteractive tests. Use `-IncludeUI` on a Windows interactive agent. Every script builds first, writes TRX output below `tests/Reports`, and returns a nonzero exit code on failure. `run-regression.ps1` also collects Coverlet coverage.

Direct category filters use xUnit traits, for example:

```powershell
dotnet test --filter "Category=Critical"
dotnet test --filter "Category=Security"
```

## Isolation

Each integration test creates a unique database below the OS temporary directory, switches the service to local host mode in memory, and deletes the entire test directory afterward. Tests disable parallel execution because authentication and path settings are process-wide state. UI automation sets `QUANLYHOSO_TEST_ROOT` only on the child process, so it cannot open the developer's configured database. Synthetic Vietnamese/Unicode records are used; no real credentials or personal data are stored in test code.

The internal `AppDataService(string databasePath)` constructor suppresses the LAN listener and is visible only to the integration-test assembly. The normal singleton constructor and application behavior are unchanged. `QUANLYHOSO_TEST_ROOT` changes paths only when explicitly set in a process environment.

## Adding tests

1. Put logic/ViewModel tests in `QuanLyHoSo.UnitTests`; mock only external boundaries.
2. Put persistence and workflow behavior in `QuanLyHoSo.IntegrationTests` and create a `TestDatabase` per test.
3. Mark tests with one or more `Category` traits such as `Smoke`, `Unit`, `Integration`, `Database`, `Security`, `Critical`, `Regression`, `Backup`, or `UI`.
4. For every confirmed defect, first add a reproducing test, verify it fails, apply the smallest behavior-preserving fix, then retain the test permanently.
5. UI tests must use AutomationId/name/control type and condition-based timeouts—never coordinates or arbitrary sleeps.

## Known gaps

Exported spreadsheet content, generated Word documents, LAN client/server protocol failures, long-running command double-click races, file locks/permission denial, and full authenticated UI workflows are not yet automated. These remain prioritized in `TEST_MATRIX.md`; UI coverage is intentionally small and separate from headless CI.
