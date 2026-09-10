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

## UI button

- Nut hanh dong chinh (them, luu, cap nhat, gui, khoi phuc, sao luu, xac nhan, quay lai/đong khi duoc dung lam CTA) phai dung `Style="{StaticResource PrimaryButton}"`.
- Nut phu/huy dung `SecondaryButton`; hanh dong nguy hiem dung `DangerButton`.
- Khong hard-code mau nen, hover, pressed hoac disabled cho nut thong thuong. Dung style chung trong `App.xaml`; `PrimaryButton` dung cung bang mau va hieu ung voi nut dang nhap (`ActionPrimaryBrush`), nhung giu goc vuong.
- Style nut rieng chi dung cho icon, card hoac man hinh xac thuc co thiet ke dac thu; neu tao style rieng van phai co du `IsMouseOver`, `IsPressed`, `IsEnabled`.

## Build

Neu task build bi khoa exe, build ra output rieng:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

`.gitignore` da ignore `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, `.lan-test-build/`.
