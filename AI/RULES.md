# Rules for AI work

Chi tiet build/git: `AI/infra/BUILD_GIT.md`.

## Doc context

- Doc `AI/INDEX.md` truoc.
- Sau do chi doc file md dung trang/chuc nang.
- Chay `git status --short --branch` truoc khi sua.
- Neu task UI: mo ViewModel + XAML cua trang do truoc.
- Neu task LAN/client/server: mo `AppPathSettings`, `LanDataClient`, `LanDataServer`, `AppDataService`.

## Sua code

- Dung `rg` de tim method/binding.
- Dung `apply_patch` khi edit.
- Khong revert thay doi user.
- Khong doc ca `AppDataService.cs`; tim method bang:

```powershell
rg -n "MethodName" Infrastructure\Data\AppDataService.cs -C 5
```

## Build

Neu task build bi khoa exe, build ra output rieng:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

`.gitignore` da ignore `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, `.lan-test-build/`.
