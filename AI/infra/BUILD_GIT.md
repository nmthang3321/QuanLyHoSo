# Infra - Build/Git

Build verify:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
dotnet build QuanLyHoSo.Server\QuanLyHoSo.Server.csproj -o .verify-builds/server
```

Notes:
- Dung output rieng de tranh exe trong `bin/Debug` bi khoa khi app dang chay.
- `.gitignore` ignore `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, `.lan-test-build/`.
- Warning `NETSDK1138` ve `.NET 5.0-windows` la warning cu.

Automated regression verification:

```powershell
.\tests\Scripts\run-smoke.ps1
.\tests\Scripts\run-all.ps1
.\tests\Scripts\run-all.ps1 -IncludeUI
```

- Scripts mac dinh build solution truoc khi test; khong test binary cu. `run-all` build mot lan roi goi cac suite voi `-SkipBuild`.
- `run-all` mac dinh khong chay UI. `-IncludeUI` can desktop Windows interactive, khong khoa man hinh.
- Test projects dung .NET 8; app van dung .NET 5. Can .NET 8 SDK va .NET 5 Desktop Runtime.
- Reports TRX/coverage nam trong `tests/Reports/`, khong commit generated reports.
- Chi tiet architecture/isolation/coverage gaps: `AI/infra/TESTING.md`.

Git:
- Luon chay `git status --short --branch` truoc khi sua/commit.
- Khong revert thay doi user.
- Neu commit, stage dung file lien quan task.
