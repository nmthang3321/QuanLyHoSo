# Coding guidelines

Updated: 2026-09-12

## Compatibility first

- Treat current UI, Vietnamese text, workflows, role rules, status strings, date formats, sort/filter semantics, exports, and SQLite values as contracts.
- Do not restyle or restructure XAML during internal refactoring. Search XAML resources, bindings, event handlers, DI/composition, serialization, and LAN DTOs before renaming or deleting code.
- If a confirmed defect requires observable change, record the old behavior, reason, new behavior, and impact in `.ai/REFACTORING_NOTES.md`.

## Placement and dependencies

- Views: `Views/<Feature>/`.
- ViewModels and commands: `ViewModels/` (retain current flat layout until a dedicated move is justified and verified).
- Presentation-only converters/behaviors: `Presentation/`.
- Shared models and wire DTOs: `Models/` and `Infrastructure/Network/LanApiModels.cs`, compiled by `QuanLyHoSo.Shared`.
- Persistence, transport, security, logging, configuration, and document generation: `Infrastructure/`, compiled by `QuanLyHoSo.Core`.
- Presentation contracts: `Application/Abstractions/`, compiled by `QuanLyHoSo.Core`.
- AI-only guidance: `.ai/`. Historical feature notes remain in `AI/`; do not place new agent instructions in production folders.

Presentation code depends on `IApplicationDataService`; pass the dependency from `ShellViewModel`. Keep default constructors only where current runtime/XAML composition needs them.

## MVVM and lifecycle

- Put presentation state and command coordination in ViewModels; keep SQLite and LAN logic out of Views/ViewModels.
- Keep genuinely visual behavior in code-behind when moving it would add indirection or risk interaction changes.
- Any ViewModel that subscribes to a longer-lived event or owns a timer must override `Dispose(bool)`, stop timers, and unsubscribe. The owner must dispose it before dropping the reference.
- Do not subscribe with an anonymous lambda when later unsubscription is required.
- Raise `PropertyChanged` only for properties whose observable value changed; preserve existing dependent-property notifications.

## Async and threading

- Use `async Task` for operations; `async void` is limited to WPF event handlers or existing command boundaries that catch their own exceptions.
- Keep UI-bound collection mutation on the dispatcher thread. Capture inputs before background work and apply results after awaiting.
- Prevent duplicate long-running actions with an existing busy flag/command state. Check disposal before applying late results to an abandoned ViewModel.
- Do not introduce `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in presentation code. The synchronous LAN facade is legacy infrastructure and should be replaced only as a coordinated, compatibility-tested change.

## Data and security

- Parameterize SQL values. Dynamic column/order fragments must come only from closed, code-owned mappings.
- Scope every connection, command, reader, transaction, stream, timer, and HTTP response.
- Enforce authorization in `AppDataService`; visibility alone is never authorization.
- LAN routes must require a validated server session except `health` and `auth/login`. Use `AuthContext.BeginRequestScope`; never set the process-global user for a request.
- Never log passwords, hashes, session tokens, request bodies containing credentials, or unnecessary personal information.
- Preserve schema and stored strings unless an explicit migration is approved and tested against a copied database.

## Errors, naming, and tests

- Log technical context and show the existing user-safe message. Do not expose stack traces in dialogs or LAN responses.
- Empty catches are allowed only for explicitly best-effort cleanup/heartbeat/logging and require a comment.
- Prefer responsibility-specific names. Avoid new `Helper`, `Manager`, or `Utils` buckets.
- Before a meaningful change: build to a separate output directory. After it: build client and server, run applicable scripts, run `PRAGMA quick_check` on copied/test data, and verify no unintended XAML/schema diff.
