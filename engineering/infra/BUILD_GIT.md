# Infrastructure - Build and Git

Build verification:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
dotnet build QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -o .verify-builds/server
```

- Use isolated output so a running app cannot lock `bin/Debug`.
- `.gitignore` excludes `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, and `.lan-test-build/`.
- `NETSDK1138` for `.NET 5.0-windows` is an existing warning.

Automated verification:

```powershell
.\tests\Scripts\run-smoke.ps1
.\tests\Scripts\run-all.ps1
.\tests\Scripts\run-all.ps1 -IncludeUI
```

- Scripts build before testing; they do not silently test stale binaries. `run-all` builds once and invokes suites with `-SkipBuild`.
- UI tests are excluded by default and require an unlocked interactive Windows desktop.
- Test projects target .NET 8; production remains .NET 5. Install the .NET 8 SDK and .NET 5 Desktop Runtime.
- Generated TRX/coverage reports under `tests/Reports/` are not committed.
- See `engineering/infra/TESTING.md` for architecture, isolation, and gaps.

Git rules:
- Run `git status --short --branch` before editing or committing.
- Never revert user changes.
- Stage only files relevant to the task.
