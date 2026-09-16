# QuanLyHoSo developer guide

A WPF .NET 5 application for managing public-service records in An Giang.

This file is the working guide for AI agents and developers. The customer-facing root `README.md` and documents under `doc/` remain in Vietnamese.

## 1. Read before changing code

Start with:

1. `AI/SESSION_HANDOFF.md` for the current working state.
2. `AI/INDEX.md` for task-specific documentation routing.
3. `AI/RULES.md` for repository rules.
4. The current source code for the affected feature.

Read customer requirements or UI references under `doc/` only when they are relevant to the task. Do not infer behavior when an existing requirement, design, or current implementation already defines it.

## 2. Architecture

The application follows a layered MVVM structure:

```text
Views
  -> ViewModels
    -> Application / Use Cases
      -> Domain
        -> Infrastructure
```

Conventions:

- Prefer MVVM and keep business logic out of XAML code-behind.
- Views bind data and commands.
- ViewModels coordinate UI state, screen-level validation, and use cases.
- Domain code contains entities, enums, and business rules.
- Infrastructure owns SQLite, file storage, export, backup, and logging.
- Do not hard-code business catalogs in the UI when they belong in data or configuration.

The solution is split into the WPF client, `QuanLyHoSo.Server`, `QuanLyHoSo.Core`, and `QuanLyHoSo.Shared`. In the normal deployment model, the server owns the database and exposes the LAN API; client workstations do not directly open the production SQLite file.

## 3. Application modules

The main modules are:

- Dashboard
- Record input
- Classification and processing
- Data export
- Settings

When adding a feature, include the relevant View, ViewModel, model/DTO, application service or use case, repository/service boundary, error handling, logging, and tests where appropriate.

## 4. Logging and issue tracing

Important operations should carry a `CorrelationId`. Logs should include, where applicable:

- Module and action
- Record code
- User or processor
- Correlation ID
- Exception stack trace

Avoid logging detailed petition or incident content that may contain sensitive information.

Audit history should cover record creation/edit/deletion, attachment changes, processing-state changes, export, backup/restore, and catalog changes.

## 5. Data and attachments

- The server uses SQLite.
- Attachment binaries are not stored directly in the database.
- Managed files are stored in an application-controlled directory; the database stores metadata and relative paths.
- Supported file types are PDF, JPG, and PNG.
- The maximum size is 10 MB per file.
- Prefer soft deletion or `IsActive = false` for records or catalog entries that must remain historically traceable.

Over LAN, attachment handling currently stores metadata/path only; physical upload or copying from a client workstation to the server has not been implemented.

## 6. Processing workflow

The business workflow is:

```text
Receive -> Classify -> Assign -> Verify -> Extend (optional) -> Await result -> Archive
```

Each processing update must preserve the old and new status, workflow step, timestamp, processor, processing content, note, and correlation ID. State transitions must be controlled by explicit rules rather than arbitrary status assignment.

Exact Vietnamese status values and user-visible labels are documented in the relevant files under `AI/` and must not be translated in code or persisted data.

## 7. Git and accounts

- Do not change global Git configuration unless explicitly requested.
- Prefer repository-local configuration when needed.
- Never store access tokens in remote URLs.
- Do not commit build/cache output from `bin/` or `obj/`.
- Preserve unrelated user changes in a dirty worktree.
- Do not revert another person's changes unless explicitly asked.

## 8. Code quality

- Use descriptive class and method names.
- Split long logic when it contains distinct business rules.
- Use asynchronous APIs for I/O, database, file, and export operations.
- Catch and log exceptions at system boundaries; do not silently swallow them.
- Validate input in both the ViewModel and application/service boundary when required.
- Avoid abstractions without a concrete need.
- Follow the existing repository style.

## 9. Tests and verification

Test projects live under `tests/` and target .NET 8; the application remains on .NET 5.

Useful commands:

```powershell
dotnet build QuanLyHoSo.sln -c Release
./tests/Scripts/run-smoke.ps1
./tests/Scripts/run-all.ps1 -IncludeUI
```

Choose verification in proportion to the change. UI changes should be rendered or exercised when practical; business-rule changes should include focused automated tests. Important coverage areas include authentication and roles, record-code generation, workflow transitions, attachments, filters/search, export, backup/restore, resubmissions, and LAN serialization.

See `AI/infra/TESTING.md`, `AI/infra/TEST_RUNBOOK.md`, and `AI/infra/TEST_MATRIX.md`.

## 10. Documentation policy

- AI/developer-facing documentation is stored under `AI/` and written in English.
- Customer-facing documentation under `doc/` remains in Vietnamese unless explicitly requested otherwise.
- Preserve Vietnamese UI labels, business statuses, database values, and exact messages even when referenced from English documentation.
- Update the relevant `AI/` document when architecture, schema, workflow, permissions, or operational behavior changes.
- Update customer documentation when the user-visible workflow changes.

Reusable Word templates are stored in `doc/templates/`.

## 11. Windows installers

The build machine needs the .NET SDK and Inno Setup 6. From the repository root, run:

```powershell
./scripts/build-release.ps1 -Version 1.0.2
```

The customer deliverables are created under `artifacts/installer/`:

- `QuanLyHoSo-Server-Setup-<version>-win-x64.exe`: install on one server machine; Administrator rights are required to register the URL and open TCP port 5055 in the firewall.
- `QuanLyHoSo-Client-Setup-<version>-win-x64.exe`: install on workstations; it does not require Administrator rights and asks for the server URL during setup.

Clients must use an address such as `http://SERVER-PC:5055` or `http://192.168.1.10:5055`, never `0.0.0.0`. Silent or scripted client installation can pass:

```powershell
QuanLyHoSo-Client-Setup-1.0.2-win-x64.exe /SERVERURL="http://SERVER-PC:5055"
```
