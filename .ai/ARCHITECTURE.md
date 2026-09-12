# QuanLyHoSo architecture

Updated: 2026-09-12

This document describes the code that exists now. It is not a target architecture.

## Solution and runtime

- `QuanLyHoSo` is the WPF desktop client (`net5.0-windows`). It owns XAML, Views, ViewModels, commands, converters, behaviors, and the composition root in `MainWindow`/`ShellViewModel`.
- `QuanLyHoSo.Shared` (`net5.0`) contains models and LAN request/response DTOs. Its project currently links source from `Models/` and `Infrastructure/Network/LanApiModels.cs`.
- `QuanLyHoSo.Core` (`net5.0`) contains the data facade, SQLite persistence, configuration, logging, security, LAN client/server transport, and document generation. Its project currently links source from `Application/` and `Infrastructure/`.
- `QuanLyHoSo.Server` is the WPF/tray host (`net5.0-windows`) for the SQLite database and LAN API.

The linked-file project layout is deliberate compatibility structure. Move linked files only as a separate, fully validated change because the client and server both depend on their current namespaces and output assemblies.

## Dependency direction

```text
QuanLyHoSo (WPF presentation)
    -> QuanLyHoSo.Core
        -> QuanLyHoSo.Shared
    -> QuanLyHoSo.Shared

QuanLyHoSo.Server
    -> QuanLyHoSo.Core
        -> QuanLyHoSo.Shared
```

The presentation-facing port is `Application/Abstractions/IApplicationDataService.cs`. `AppDataService` implements it and remains the single local/LAN facade. `ShellViewModel` receives this contract once and passes it to feature ViewModels. Default constructors still compose `AppDataService.Instance`, preserving existing creation behavior and XAML compatibility.

## Client startup and navigation

```text
App.OnStartup
  -> register global exception logging
  -> configure UI dispatch for data-service events
MainWindow
  -> ShellViewModel(AppDataService.Instance)
  -> LoginViewModel
successful login
  -> AuthContext.SignIn
  -> role-dependent first page
  -> ContentControl selects a View through App.xaml DataTemplates
```

`ShellViewModel` owns navigation state and lazily creates page ViewModels. A direct sidebar click intentionally resets the destination ViewModel so temporary filters/dialog state returns to the existing default. Internal edit/classify/back flows reuse ViewModels and retain the established sidebar selection behavior.

Page ViewModels implement the lifecycle inherited from `ViewModelBase`. Before a reset, sign-out, or window close, the shell disposes owned ViewModels. ViewModels that subscribe to `AppDataService.CatalogChanged`, row/collection events, or `DispatcherTimer` ticks detach them during disposal.

## View/ViewModel relationships

- `LoginView` -> `LoginViewModel`
- `RequiredPasswordChangeView` -> `RequiredPasswordChangeViewModel`
- `DashboardView` -> `DashboardViewModel`
- `RecordInputView` -> `RecordInputViewModel`
- `RecordListView` -> `RecordListViewModel` (owns `RecordTrashViewModel`)
- `RecordProcessingView` -> `RecordProcessingViewModel`
- `StaffTrackingView` -> `StaffTrackingViewModel`
- `SettingsView` -> `SettingsViewModel`
- `SettingsGuideView` -> `SettingsGuideViewModel`

Code-behind is retained where behavior is view-specific: focus, scrolling, chart geometry, drag/drop, password-box synchronization, dynamic area selector controls, and file dialogs. Business persistence remains outside Views.

## Data flow

```text
View
  -> ICommand / binding
  -> feature ViewModel
  -> IApplicationDataService
  -> AppDataService
       Client mode -> LanDataClient -> LAN API -> AppDataService on server
       AdminHost   -> SQLite directly
  -> SQLite
```

The default desktop mode is `Client`. The standalone server calls `AppPathSettings.UseServerMode`, initializes schema/seeds, and starts `LanDataServer`. SQLite commands use parameters for user-controlled values; connections, commands, readers, and transactions are scoped with `using`.

## Authentication and authorization

- Passwords in SQLite use PBKDF2-SHA256 with a per-password random salt and fixed-time verification.
- “Remember me” keeps the existing JSON location and field names, but password contents use Windows DPAPI with `CurrentUser` scope. Legacy Base64 values are migrated when loaded.
- A successful LAN login issues a random, server-memory session token. Every non-health/non-login LAN request must present it.
- The server maps the token to a user ID and reloads the active user and role from SQLite. It does not trust role/display-name fields supplied by a client.
- `AuthContext.BeginRequestScope` isolates concurrent LAN identities with `AsyncLocal`; the WPF application continues to use its normal signed-in user.
- Authorization is enforced again in `AppDataService` for admin settings, record create/delete/edit, record ownership, and processing transitions.

Sessions expire after 12 hours of inactivity and are cleared when the server stops. The LAN transport is currently HTTP; deploy only on a trusted, access-controlled LAN until TLS is configured.

## Persistence

The server database contains:

- `Users`
- `Areas`
- `CatalogItems`
- `Records`
- `RecordAttachments`
- `ProcessHistories`
- `SystemLogs`
- `LeadershipNotices`
- `LeadershipKpiTargets`

`AppDataService.Initialize` creates/repairs the schema, seeds an empty database, normalizes legacy values, creates indexes, and starts the LAN listener in host mode. This refactor did not change schema SQL, table/column names, stored values, migrations, record numbering, filtering, sorting, or status rules.

## Export, documents, backup, and restore

- Excel export is coordinated by `RecordListViewModel`; filtered rows come from `AppDataService`, while Open XML package writing runs off the UI thread.
- Initial result documents are generated by `InitialResultDocumentGenerator` using the existing `.docx` templates under `doc/`.
- Backup uses SQLite's online backup API rather than copying a live database file.
- Client backup creates a safe server-side backup and then downloads it.
- Restore validates the uploaded database, creates a safety backup, restores with SQLite's backup API, runs `PRAGMA quick_check`, and removes the upload temp file.

## Errors and logging

`App` registers dispatcher, AppDomain, and unobserved-task handlers. `AppLogger` writes UTF-8 daily logs under the configured log folder and retains 30 days. User-facing dialogs remain in the presentation layer and preserve their existing text/flow. Logging failures are intentionally best-effort so they cannot break primary workflows.

## Extension points

- Add a screen by creating its View/ViewModel, registering the DataTemplate in `App.xaml`, and adding navigation in `ShellViewModel`.
- Add presentation data operations to `IApplicationDataService` and implement both local and client routing in `AppDataService` plus a server route/DTO where required.
- Add a catalog by following the existing catalog type flow; preserve stored string values because they are business data.
- Split `AppDataService` by feature only through behavior-preserving extraction. It remains a large facade and persistence implementation; avoid adding another manager/service layer around it without a concrete need.
