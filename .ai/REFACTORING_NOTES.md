# Architecture review and refactoring notes

Review date: 2026-09-12

## Behavioral baseline

The compatibility baseline is the pre-refactor implementation plus the screen/workflow notes under `AI/`. The review traced startup, DataTemplates, navigation reset/reuse behavior, authentication, role checks, CRUD, record ownership, processing transitions, search/filter/sort/paging, export, attachments, notices/KPI, settings, backup/restore, update download, logging, LAN routing, database initialization, and shutdown.

No XAML file was changed. No database schema, migration, SQL filter/order rule, business status, role rule, user-facing label/message, document template, export column, or saved business-data format was changed.

## Findings fixed

### CRITICAL — LAN identity spoofing and cross-request authorization race

Old defect: every request supplied a complete `AppUser`, including its role, and the server trusted it. Concurrent request tasks also wrote that identity into one process-global `AuthContext`, so identities could overlap. A LAN caller could forge admin fields; two legitimate clients could receive the wrong authorization scope under timing overlap.

Fix: successful login now issues a cryptographically random in-memory session token. Each later request validates the token, maps it to a user ID, reloads the active user/role from SQLite, and executes inside an `AsyncLocal` request scope.

Necessary behavior change: unauthenticated/forged API calls are rejected. Sessions expire after 12 hours of inactivity and server restart requires login again. Normal in-app login and authorized workflows are unchanged.

### CRITICAL — remembered password stored as reversible plaintext

Old defect: `login-remember.json` Base64-encoded the password, which provides no confidentiality.

Fix: the existing `PasswordText` field now contains Windows DPAPI `CurrentUser` ciphertext with a `dpapi:` marker. Existing Base64 values remain readable and are migrated immediately on load.

Necessary behavior change: another Windows account/machine cannot decode the remembered password. UI behavior, checkbox semantics, file location, and JSON property names are unchanged.

### HIGH — ViewModel/event/timer retention across navigation resets

Old defect: direct sidebar navigation intentionally created new ViewModels, while old instances remained subscribed to singleton `CatalogChanged` events or dispatcher timers. They stayed alive and continued reacting, causing duplicate work and steadily increasing memory use.

Fix: `ViewModelBase` now has an idempotent disposal lifecycle. `ShellViewModel` disposes pages on reset/sign-out/window close. Affected feature ViewModels stop timers and detach service, collection, column, and row events. `MainWindow` disposes its DataContext.

Observable behavior is unchanged; abandoned pages no longer receive callbacks. A disposed dashboard suppresses a late, irrelevant failure popup from work started before navigation.

### MEDIUM — malformed password hash could crash authentication

Old defect: corrupt Base64/hash metadata in SQLite could throw from password verification.

Fix: malformed hashes now fail authentication safely. Valid PBKDF2 hashes and password rules are unchanged.

### MEDIUM — process-lifetime resources lacked explicit shutdown

Fix: the LAN client heartbeat timer/`HttpClient`, LAN cancellation source, and data facade are stopped/disposed during application/server shutdown. Server tray behavior and explicit-exit workflow are unchanged.

### MEDIUM — presentation directly coupled to the singleton data implementation

Fix: `IApplicationDataService` is the presentation-facing port. `ShellViewModel` receives it once and passes it to feature ViewModels. Existing default constructors remain as compatibility composition paths. No DI framework or extra service chain was added.

### LOW — obsolete commented implementation

Removed a large commented-out duplicate leadership-notification query after confirming that the active method and all call sites use the newer implementation.

## Risks intentionally not changed in this pass

### HIGH — HTTP LAN transport

Credentials and session tokens travel over plain HTTP on the configured LAN. Session validation prevents role forgery but not network interception. A production deployment should terminate TLS with a managed certificate or place the service behind an authenticated encrypted tunnel. This needs deployment/certificate decisions and cannot be introduced without changing configuration and connectivity behavior.

### HIGH — attachment paths are workstation-local

Attachments currently persist a `FilePath` string. A file chosen on one client may not exist on the server or another client. Correcting this needs an approved upload/storage/download contract and migration policy; silently copying or rewriting paths would change saved-data and output behavior.

### HIGH — .NET 5 is out of support

Both WPF executables intentionally remain on `net5.0-windows` to satisfy compatibility requirements. Builds emit `NETSDK1138`. Plan a separate framework/package migration with installer and regression testing.

### MEDIUM — synchronous LAN facade can block the UI

`LanDataClient` is synchronous, and several navigation/load paths call it from the UI thread. Some expensive flows already use `Task.Run`, but a full async transport conversion would alter ordering, command timing, and error propagation across most screens. Perform it feature-by-feature with characterization tests.

### MEDIUM — `AppDataService` remains a large facade/persistence class

The contract boundary now permits tests and future extraction, but splitting roughly 5,000 lines was not done mechanically. Feature extraction should follow actual transaction/query cohesion, with database characterization coverage, rather than introducing repository/service/manager chains.

### LOW — dialog/file/process APIs remain in ViewModels

This is testability debt, especially in Settings and export flows. Moving it needs an interaction abstraction that preserves every current dialog caption, default button, path, and timing. It is not a runtime defect.

## Verification performed

- Restored packages without upgrading the target framework or existing SQLite package.
- Built WPF client to isolated output: 0 errors; only expected `NETSDK1138`.
- Built WPF server to isolated output: 0 errors; only expected `NETSDK1138`.
- Started the refactored server against a fresh isolated SQLite database on port 5077.
- Verified health, `admin/admin123` login, session issuance, authenticated catalog call (119 areas), rejection of a forged admin identity without a token, and `PRAGMA quick_check = ok`.
- Verified the sample database separately with `PRAGMA quick_check = ok`; counts remained 9 users, 105 records, 37 catalog items, and 119 areas.
- Confirmed `git diff --name-only -- '*.xaml'` is empty.
- Confirmed the refactor diff contains no schema-changing SQL.

Manual pixel-level and full click-through UI regression still require an interactive Windows desktop test session. The unchanged XAML and preserved bindings provide static assurance but do not replace that acceptance pass.

## Approved feature — resubmitted resolved records (2026-09-13)

User explicitly approved a behavior and schema change. Previously intake warned about one exact match within ±30 days and could still create an ordinary processing record. Intake now displays sender history across all dates, with normalized name + phone (or name + address when both phones are absent). The officer explicitly chooses ordinary intake or a linked resubmission of an already resolved case and supplies a reason.

A resubmission receives a separate record code, intake date, content and attachments, with the status `Đã giải quyết — hồ sơ gửi lại`. It references the original result without manufacturing processing history or a new resolution date. It contributes to received totals and its own status category, but not processing queues, overdue counts or completed-work KPIs. Record-list details show sender history and links to individual records; processing actions are hidden/blocked for repeats.

Additive metadata columns (`SenderId`, `OriginalRecordCode`, `ResubmissionReason`) default to empty for legacy records; indexes support identity/reference lookup. Links are immutable through ordinary edits. Referenced originals cannot be deleted, renamed, changed to a different sender/case or reopened while linked records exist. Server-side validation enforces identity, area, case type, resolved status and live references. All new LAN routes require existing authenticated sessions; sender history respects officer access.

Verification: client/server isolated builds and Release solution build passed; `tests/Scripts/run-all.ps1 -IncludeUI` passed 71 cases (37 unit/ViewModel, 33 integration, 1 UI smoke). New checks include normalization, independent intake preservation, no extra work/resolution, invalid references, edit identity, reference preservation, copied legacy migration/idempotence/quick_check and authenticated LAN round trip. Dialog/detail rendered using synthetic data; full authenticated dialog click-through remains manual. No real user database was migrated during development.

## Requested popup correction (2026-09-13)

The user requested an in-page popup instead of a separate Window and reported that resubmission could not be selected. SenderHistoryDialog is now a UserControl hosted as a modal overlay in RecordInputView, coordinated through ViewModel commands and a captured pending draft. The form is disabled behind the overlay. Selection defaults to the first eligible resolved original rather than the newest row, which may be a repeat or unresolved intake. Eligibility explanations and validation/save errors are displayed inline. The existing server eligibility rules remain unchanged.

Verification: Release client/server solution build and 74 cases passed (40 unit/ViewModel, 33 integration, 1 UI smoke). New popup tests cover original selection, required reason, successful resubmission/new-record continuation, and cancel preserving input. A synthetic WPF harness rendered the actual overlay and verified button binding, eligible/ineligible selection and save completion with a mock service.

## Requested detail grouping correction (2026-09-13)

Record-list detail previously showed all records of the sender, including independent new intakes. The user requested separate management of each new intake. Detail history now includes only its original record and resubmissions explicitly linked to that original. A new intake without resubmissions shows itself only. Intake lookup still provides all sender records for choosing an original. The caption is now “Hồ sơ gốc và các lần gửi lại”. A regression test covers two independent groups with identical sender/area/case data and ensures opening either original or repeat shows only its own group.
