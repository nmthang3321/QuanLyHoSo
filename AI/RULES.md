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

- Neu task thay doi code/logic, doc `AI/infra/TESTING.md` va chay `tests/Scripts/run-smoke.ps1` sau thay doi.
- Truoc merge/release, chay `tests/Scripts/run-all.ps1`; UI tests tach rieng, can desktop Windows khong khoa.
- Khong sua UI/business rules chi de test pass. Bao cao defect hien co va test fail; khong an failure bang rerun hoac assertion yeu.
- Test code phai nam trong `tests/`; khong dung DB, credentials hay file ca nhan/production lam test data.

Neu task build bi khoa exe, build ra output rieng:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

`.gitignore` da ignore `.verify-build/`, `.verify-build-*/`, `.verify-builds/`, `.lan-test-build/`.
