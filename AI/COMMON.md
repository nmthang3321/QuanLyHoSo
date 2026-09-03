# Common - QuanLyHoSo

## Tong quan

- App: WPF desktop, C#, MVVM tu viet, `.NET 5.0-windows`.
- Projects: `QuanLyHoSo` WPF client/admin UI, `QuanLyHoSo.Core` data/network/config/logging/security/doc generation, `QuanLyHoSo.Shared` models/DTO, `QuanLyHoSo.Server` console LAN server.
- DB: SQLite tren may server.
- App WPF mac dinh chay `Client`, lay du lieu qua `QuanLyHoSo.Server` theo `AdminServerUrl`.
- Server DB mac dinh: `%LocalAppData%\QuanLyHoSo\Data\quanlyhoso.db`.
- Server/client log mac dinh: `%LocalAppData%\QuanLyHoSo\Logs\quanlyhoso-yyyyMMdd.log`.
- Settings path: `%LocalAppData%\QuanLyHoSo\Settings\path-settings.json`.
- Data service chinh: `Infrastructure\Data\AppDataService.cs`.
- Shell/navigation: `ViewModels\ShellViewModel.cs`, `MainWindow.xaml`.
- Logger: `Infrastructure\Logging\AppLogger.cs`.

## Role va phan quyen

File chinh:
- `Models\AuthModels.cs`
- `Infrastructure\Security\AuthContext.cs`
- `ViewModels\LoginViewModel.cs`
- `Views\Auth\LoginView.xaml`

Role:
- `Admin`: toan quyen, bao gom nhap/sua/xoa ho so qua server khi WPF chay client.
- `Leader`: chi xem, khong sua.
- `Officer`: xem, chinh sua, phan loai/xu ly ho so co `Records.ProcessorName == AuthContext.CurrentDisplayName`; khong duoc them moi/xoa ho so.

Dang co login, logout, quan ly user trong Settings. User mac dinh seed: `admin/admin123`.

## Verify

Lenh nen dung:

```powershell
dotnet build QuanLyHoSo.csproj -o .verify-builds/current
```

Warning `NETSDK1138` ve `.NET 5.0-windows` het support la warning cu.
