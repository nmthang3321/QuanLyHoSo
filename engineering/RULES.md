# Rules for maintenance work

See `engineering/infra/BUILD_GIT.md` for build and Git details.

## Documentation context

- Read `engineering/INDEX.md` first, then only the page/feature files relevant to the task.
- Run `git status --short --branch` before editing.
- For UI work, inspect the page ViewModel and XAML first.
- For LAN/client/server work, inspect `AppPathSettings`, `LanDataClient`, `LanDataServer`, and the relevant `AppDataService` methods.
- Files under `engineering/` are written in English. The root `README.md` and customer-facing documents under `doc/` remain in Vietnamese unless explicitly requested otherwise.

## Code changes

- Use `rg` to locate methods and bindings.
- Use `apply_patch` for edits.
- Never revert user changes.
- Do not read all of `AppDataService.cs`; locate the method first:

```powershell
rg -n "MethodName" Infrastructure\Data\AppDataService.cs -C 5
```

## UI buttons

- Primary actions use `Style="{StaticResource PrimaryButton}"`.
- Secondary/cancel actions use `SecondaryButton`; destructive actions use `DangerButton`.
- Do not hard-code normal button background, hover, pressed, or disabled colors. Use shared `App.xaml` styles. `PrimaryButton` shares the login action palette (`ActionPrimaryBrush`) while keeping square corners.
- Dedicated styles are allowed for icons, cards, or specialized authentication screens, but must define `IsMouseOver`, `IsPressed`, and `IsEnabled` states.

## Build and tests

- For code or logic changes, read `engineering/infra/TESTING.md` and run `tests/Scripts/run-smoke.ps1` afterward.
- Before merge/release, run `tests/Scripts/run-all.ps1`; UI tests require an unlocked interactive Windows desktop.
- Do not change UI or business behavior merely to make a test pass. Report existing defects and failures; do not hide them with reruns or weaker assertions.
- Keep tests under `tests/` and never use production databases, credentials, or personal files as fixtures.
- If running executables lock build output, use an isolated directory:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

`.gitignore` excludes `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, and `.lan-test-build/`.
