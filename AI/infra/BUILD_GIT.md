# Infra - Build/Git

Build verify:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

Notes:
- Dung output rieng de tranh exe trong `bin/Debug` bi khoa khi app dang chay.
- `.gitignore` ignore `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, `.lan-test-build/`.
- Warning `NETSDK1138` ve `.NET 5.0-windows` la warning cu.

Git:
- Luon chay `git status --short --branch` truoc khi sua/commit.
- Khong revert thay doi user.
- Neu commit, stage dung file lien quan task.
